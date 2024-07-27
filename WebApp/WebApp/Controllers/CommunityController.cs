using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Community;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using System.Security.Claims;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Bd.Enums;

namespace WebApp.Controllers
{
    /// <summary>
    /// Author: Antoine Bélanger and Sphero
    /// Description: Controller for handling community-related functionalities such as posts and messages.
    /// </summary>
    public class CommunityController : Controller
    {
        private readonly Context _context;

        public CommunityController(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves and displays the latest 20 posts in the community.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.ChartSettings)  // Include ChartSettings
                .Include(p => p.Stock) // Include Stock
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            AppUser user = await getUser();

            var model = new CommunityVM
            {
                User = user,
                Posts = posts
            };

            return View(model);
        }


        /// <summary>
        /// Displays the create post view.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }


        /// <summary>
        /// Handles the creation of a new post.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreatePostVM model)
        {
            AppUser user = await getUser();

            if (ModelState.IsValid)
            {
                var post = new Post
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedAt = GetEasternTime(),
                    UpdatedAt = GetEasternTime(),
                    User = user,
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }



        /// <summary>
        /// Retrieves and displays the details of a specific post.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var post = await _context.Posts
                .Include(p => p.Messages)      // Include Messages related to the Post
                    .ThenInclude(m => m.User)  // Include User related to each Message
                .Include(p => p.User)          // Include User related to the Post
                .Include(p => p.ChartSettings) // Include ChartSettings
                .Include(p => p.Stock) // Include Stock
                .FirstOrDefaultAsync(p => p.Id == id);

            AppUser user = await getUser();

            if (post == null)
            {
                return NotFound();
            }

            var model = new CreateMessageVM
            {
                post = post,
                user = user
            };

            // Pass the chart settings ID to the view
            ViewData["ChartSettingsId"] = post.ChartSettings?.Id ?? Guid.Empty;
            return View(model);
        }

        /// <summary>
        /// Loads a specific page of posts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LoadPosts(int pageNumber = 1, int pageSize = 20)
        {
            pageSize = 20;
            var posts = await _context.Posts
                 .Include(p => p.User)
                 .Include(p => p.ChartSettings)  // Include ChartSettings
                 .Include(p => p.Stock) // Include Stock
                 .OrderByDescending(p => p.CreatedAt)
                 .Skip((pageNumber - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

            return PartialView("_PostsPartial", posts);
        }

        /// <summary>
        /// Handles the creation of a new message in a specific post.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Details(CreateMessageVM model)
        {

            AppUser user = await getUser();

            var post = await _context.Posts
                                     .Include(p => p.Messages)
                                     .Include(p => p.User)
                                     .Include(p => p.ChartSettings)  // Include ChartSettings
                                     .Include(p => p.Stock) // Include Stock
                                     .FirstOrDefaultAsync(p => p.Id == model.post.Id);

            if (post == null)
            {
                return NotFound();
            }

            var message = new Message
            {
                Content = model.text,
                CreatedAt = GetEasternTime(),
                User = user
            };

            post.Messages.Add(message);
            await _context.SaveChangesAsync();

            model.post = post;
            model.user = user;
            // Pass the chart settings ID to the view
            ViewData["ChartSettingsId"] = post.ChartSettings?.Id ?? Guid.Empty;

            return View(model);
        }

        /// <summary>
        /// Searches for posts based on a search term.
        /// </summary>
        public async Task<IActionResult> SearchPosts(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            IQueryable<Post> postsQuery; 

            // Validate and sanitize searchTerm as needed
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                // If searchTerm is empty or contains only spaces, retrieve all posts
                postsQuery = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.ChartSettings)  // Include ChartSettings
                    .Include(p => p.Stock) // Include Stock
                    .Include(p => p.Likes)
                    .OrderByDescending(p => p.CreatedAt);
            }
            else
            {
                // Otherwise, search posts based on searchTerm
                postsQuery = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.ChartSettings)  // Include ChartSettings
                    .Include(p => p.Stock) // Include Stock
                    .Where(p => p.Title.Contains(searchTerm) || p.Content.Contains(searchTerm))
                    .OrderByDescending(p => p.CreatedAt);
            }


            // Paginate the search results
            var posts = await postsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return PartialView with filtered posts
            return PartialView("_PostsPartial", posts);
        }




        /// <summary>
        /// Retrieves the currently logged-in user.
        /// </summary>
        public async Task<AppUser> getUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            AppUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

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
        /// <summary>
        /// Parses the chart settings from a JSON string.
        /// </summary>
        private ChartSettings ParseChartSettings(string json)
        {
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            var settings = new ChartSettings
            {
                Id = root.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.GetString(), out var id) ? id : Guid.Empty,
                Symbol = root.GetProperty("symbol").GetString(),
                Interval = root.GetProperty("interval").GetString(),
                ChartType = root.GetProperty("chartType").GetString(),
                TimeZone = root.GetProperty("timeZone").GetString(),
                Theme = root.GetProperty("theme").GetString(),
                LineColor = root.GetProperty("lineColor").GetString(),
                UpColor = root.GetProperty("upColor").GetString(),
                DownColor = root.GetProperty("downColor").GetString(),
                Drawings = root.GetProperty("drawings").EnumerateArray()
                    .Select(d => new Drawing
                    {
                        Type = d.GetProperty("type").GetString(),
                        Coordinates = d.GetProperty("coordinates").GetString()
                    })
                    .ToList()
            };

            return settings;
        }
    }

    /// <summary>
    /// Author: Antoine Bélanger and Sphero
    /// Description: API Controller for handling actions related to posts such as likes and creation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PostApiController : ControllerBase
    {
        private readonly Context _context;
        private readonly UserManager<AppUser> _userManager;
        private async Task<AppUser> getUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            AppUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }

        public PostApiController(Context context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        /// <summary>
        /// Handles liking and unliking of posts.
        /// </summary>
        [HttpPost("HandleLikes")]
        public async Task<IActionResult> HandleLikes(Guid postId)
        {
            var user = await getUser();

            if (user == null)
            {
                return Unauthorized();
            }

            var post = await _context.Posts
                                    .Include(p => p.Messages)
                                    .Include(p => p.User)
                                    .Include(p => p.Likes)
                                    .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound();
            }
            var existingLike = post.Likes.FirstOrDefault(l => l.UserId == user.Id);
            bool isLiked = false;

            if (existingLike != null)
            {
                // Unlike the post
                post.Likes.Remove(existingLike);
            }
            else
            {
                // Like the post
                var newLike = new Like
                {
                    UserId = user.Id,
                    PostId = post.Id
                };

                post.Likes.Add(newLike);
                _context.likes.Add(newLike);
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            var likesCount = post.Likes.Count;

            return Ok(new { likesCount = post.Likes.Count, isLiked });
        }

        /// <summary>
        /// Handles the creation of a new post via API.
        /// </summary>
        [HttpPost("Create")]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("User not logged in.");
            }

            var userId = Guid.Parse(userIdString);

            AppUser user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Unauthorized();
            }

            if (!Enum.TryParse(model.PostType, out PostType postType))
            {
                return BadRequest("Invalid PostType value.");
            }
            // Create the post
            var post = new Post
            {
                Title = model.Title,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Type = postType,
                User = user
            };

            // If the user wants to include a chart, parse and save the ChartSettings
            if (model.IncludeChart && !string.IsNullOrEmpty(model.ChartSettingsJson))
            {
                var chartSettings = ParseChartSettings(model.ChartSettingsJson);
                chartSettings.UserId = null;

                // Fetch the stock based on the chart settings symbol
                var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Name == chartSettings.Symbol);
                if (stock == null)
                {
                    return BadRequest("Stock not found.");
                }

                // Assign the stock to the post
                post.Stock = stock;

                _context.ChartSettings.Add(chartSettings);
                await _context.SaveChangesAsync();

                // Optionally associate the chart settings with the post
                post.ChartSettingsId = chartSettings.Id;
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Parses the chart settings from a JSON string.
        /// </summary>
        private ChartSettings ParseChartSettings(string json)
        {
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            return new ChartSettings
            {
                Id = root.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.GetString(), out var id) ? id : Guid.Empty,
                Symbol = root.GetProperty("symbol").GetString(),
                Interval = root.GetProperty("interval").GetString(),
                ChartType = root.GetProperty("chartType").GetString(),
                TimeZone = root.GetProperty("timeZone").GetString(),
                Theme = root.GetProperty("theme").GetString(),
                LineColor = root.GetProperty("lineColor").GetString(),
                UpColor = root.GetProperty("upColor").GetString(),
                DownColor = root.GetProperty("downColor").GetString(),
                Drawings = root.GetProperty("drawings").EnumerateArray()
                    .Select(d => new Drawing
                    {
                        Type = d.GetProperty("type").GetString(),
                        Coordinates = d.GetProperty("coordinates").GetString()
                    })
                    .ToList()
            };
        }
    }
}

