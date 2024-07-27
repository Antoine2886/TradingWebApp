using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp.ViewModels;
using IdentityCore.ViewModels.Account;
using IdentityCore.Infrastructure;
using System.Security.Claims;
using Bd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApp.ViewModels.Profile;
using System.Text.Json;

namespace WebApp.Controllers
{
    /// <summary>
    /// Author: Sphero
    /// Description: Controller for handling the main functionalities of the application including user profile and friendship management.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly Context _context;
        private readonly UserManager<AppUser> UserManager;

        public HomeController(Context context, UserManager<AppUser> userManager)
        {
            _context = context;
            UserManager = userManager;
        }

        /*public IActionResult Index()
        {
            return View();
        } */

        /// <summary>
        /// Retrieves and displays the home page for the logged-in user.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            AppUser user = await getUser();

            return View(user);
        }


        /// <summary>
        /// Displays the profile of the user with the specified visible name.
        /// </summary>
        public async Task<IActionResult> Profile(string visibleName)
        {
            AppUser loggedin_user = await getUser();

            AppUser user = await _context.Users.Include(u => u.Posts).FirstOrDefaultAsync(u => u.VisibleName == visibleName);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileVM
            {
                loggedin_user = loggedin_user,
                user_viewing = user
            };

            return View(model);
        }

        /// <summary>
        /// Displays the privacy policy page.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }
        /// <summary>
        /// Displays the error page with the request ID.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        /// <summary>
        /// Handles the follow action, allowing the logged-in user to follow another user.
        /// </summary>
        //[HttpPost]
        public async Task<IActionResult> Follow(string username)
        {
            var loggedInUser = await getUser();

            // Retrieve the user to whom will be followed
            var user = await _context.Users
                .Include(u => u.Friends) // Ensure Friends are included for the current user
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null || loggedInUser == null)
            {
                return NotFound();
            }

            var existingRequest = await _context.Friends
                .FirstOrDefaultAsync(uf => uf.UserId == loggedInUser.Id && uf.FriendId == user.Id && uf.Status == "follow");

            if (existingRequest != null)
            {
                // Handle the case where the friend request already exists
                TempData["Message"] = "Friend request already sent.";
                return RedirectToAction("Index");
            }

            // Add new relationships
            var userFollow = new UserFriend
            {
                UserId = loggedInUser.Id,
                FriendId = user.Id,
                Status = "follow",
                user = loggedInUser,
                friend = user
            };

            var userFollowed = new UserFriend
            {
                UserId = user.Id,
                FriendId = loggedInUser.Id,
                Status = "followed",
                user = user,
                friend = loggedInUser
            };

            // Add to the context
            _context.Friends.Add(userFollow);
            _context.Friends.Add(userFollowed);

            // Create a notification
            var notification = new Notification
            {
                Type = "Follow",
                Message = $"{loggedInUser.VisibleName} started following you.",
                Timestamp = DateTime.UtcNow
            };

            // Ensure notifications are properly initialized and added
            if (user.notifications == null)
            {
                //user.notifications = new List<string>();
            }

            //user.notifications.Add(JsonSerializer.Serialize(notification));

            // Update the user
            _context.Users.Update(user);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        /// <summary>
        /// Sends a friend request from the logged-in user to another user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(Guid id)
        {
            var loggedInUser = await getUser();

            // Retrieve the user to whom the friend request will be sent
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null || loggedInUser == null)
            {
                return NotFound();
            }

            // Check if a friend request has already been sent
            var existingRequest = await _context.Friends
                .FirstOrDefaultAsync(uf => uf.UserId == loggedInUser.Id && uf.FriendId == user.Id && uf.Status == "friend-requested");

            if (existingRequest != null)
            {
                // Handle the case where the friend request already exists
                // You could notify the user or redirect with a message
                // For example:
                TempData["Message"] = "Friend request already sent.";
                return RedirectToAction("Index");
            }

            // Create a new outgoing friend request
            var outgoing_userfriend = new UserFriend
            {
                UserId = loggedInUser.Id,
                FriendId = user.Id,
                Status = "outgoing-friend-request",
                user = loggedInUser,
                friend = user
            };

            // Create a new incoming friend request
            var incoming_userfriend = new UserFriend
            {
                UserId = user.Id,
                FriendId = loggedInUser.Id,
                Status = "incoming-friend-request",
                user = user,
                friend = loggedInUser
            };

            // Add in user database
            loggedInUser.Friends.Add(outgoing_userfriend);
            user.Friends.Add(incoming_userfriend);

            // Create a notification for the recipient
            var notification = new Notification
            {
                Type = "Friend Request",
                Message = $"{loggedInUser.VisibleName} sent you a friend request.",
                Timestamp = DateTime.UtcNow
            };

            // Add the notification to the recipient’s notifications list
            if (user.notifications == null)
            {
               // user.notifications = new List<string>();
            }

            var notificationJson = JsonSerializer.Serialize(notification);
            //user.notifications.Add(notificationJson);

            // Update the recipient user
            _context.Users.Update(user);

            // Add the friend request to the database
            _context.Friends.Add(outgoing_userfriend);
            _context.Friends.Add(incoming_userfriend);

            

            // Save changes to the context
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Accepts a friend request from another user.
        /// </summary>
        //[HttpGet]
        public async Task<IActionResult> AcceptFriendRequest(Guid id)
        {
            var loggedInUser = await getUser();
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(uf => uf.UserId == id && uf.FriendId == loggedInUser.Id && uf.Status == "outgoing-friend-request");
            var existingRequest = await _context.Friends
                .FirstOrDefaultAsync(uf => uf.UserId == loggedInUser.Id && uf.FriendId == id && uf.Status == "incoming-friend-request");


            if (friendRequest == null || existingRequest == null)
            {
                return NotFound();
            }

            // Updating friend connection on both user sides, from requested to friend 
            // must delete the existing info first since Status is part of the composite key, and then re-add the object into the table

            _context.Friends.Remove(friendRequest);
            var friend = new UserFriend
            {
                UserId = id,
                FriendId = loggedInUser.Id,
                Status = "Friend",
                user = friendRequest.user,
                friend = loggedInUser
            };
            _context.Friends.Add(friend);


            _context.Friends.Remove(existingRequest);
            var friend2 = new UserFriend
            {
                UserId = existingRequest.UserId,
                FriendId = existingRequest.FriendId,
                Status = "Friend",
                user = existingRequest.user,
                friend = existingRequest.friend,
            };
            _context.Friends.Add(friend2);


            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "Friend request accepted.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Ignores a friend request from another user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> IgnoreFriendRequest(Guid id)
        {
            var loggedInUser = await getUser();
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(uf => uf.UserId == id && uf.FriendId == loggedInUser.Id && uf.Status == "friend-requested");

            if (friendRequest == null)
            {
                return NotFound();
            }

            _context.Friends.Remove(friendRequest);

            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "Friend request ignored.";
            return RedirectToAction("Index");
        }


        /// <summary>
        /// Retrieves the currently logged-in user.
        /// </summary>
        public async Task<AppUser> getUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            AppUser user = await _context.Users
                .Include(u => u.Friends)
                    .ThenInclude(f => f.friend)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }
        /// <summary>
        /// Retrieves the current Eastern Time.
        /// </summary>
        public static DateTime GetEasternTime()
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime utcNow = DateTime.UtcNow;
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternZone);
            return easternTime;
        }

    }
}