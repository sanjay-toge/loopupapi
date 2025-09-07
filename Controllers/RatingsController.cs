using LoopUpAPI.Models;
using LoopUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LoopUpAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly RatingService _ratingService;

        public RatingsController(RatingService ratingService)
        {
            _ratingService = ratingService;
        }

        /// <summary>
        /// Submit a rating request (goes into pending state until accepted).
        /// </summary>
        [HttpPost("request")]
        public async Task<IActionResult> AddRatingRequest([FromBody] Rating rating)
        {
            try
            {
                await _ratingService.AddRatingRequestAsync(rating);
                return Ok(new { message = "Rating request submitted. Waiting for acceptance." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Accept a pending rating (moves it to latest ratings and updates user’s average).
        /// </summary>
        [HttpPost("accept/{pendingRatingId}")]
        public async Task<IActionResult> AcceptRating(string pendingRatingId)
        {
            try
            {
                await _ratingService.AcceptRatingAsync(pendingRatingId);
                return Ok(new { message = "Rating accepted and applied." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reject a pending rating (deletes it).
        /// </summary>
        [HttpPost("reject/{pendingRatingId}")]
        public async Task<IActionResult> RejectRating(string pendingRatingId)
        {
            await _ratingService.RejectRatingAsync(pendingRatingId);
            return Ok(new { message = "Rating rejected and removed." });
        }

        /// <summary>
        /// Get accepted ratings for a user.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRatings(string userId)
        {
            var ratings = await _ratingService.GetRatingsForUserAsync(userId);
            return Ok(ratings);
        }

        /// <summary>
        /// Get pending ratings for a user (to review/accept/reject).
        /// </summary>
        [HttpGet("pending/{userId}")]
        public async Task<IActionResult> GetPendingRatings(string userId)
        {
            var ratings = await _ratingService.GetPendingRatingsForUserAsync(userId);
            return Ok(ratings);
        }

        [HttpGet("recent-given")]
        public async Task<IActionResult> GetRecentRatingsByUser([FromQuery] int limit = 10)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Invalid token" });


            var ratings = await _ratingService.GetRecentlyRatedUsersAsync(userId, limit);
            return Ok(ratings);
        }
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            var count = await _ratingService.GetCountOfPeopleIRatedAsync(userId);
            return Ok(new { count });
        }

        [HttpGet("mutual")]
        public async Task<IActionResult> GetMutual()
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            var count = await _ratingService.GetMutualRatingsCountAsync(userId);
            return Ok(new { mutualRatings = count });
        }

        [HttpGet("distribution")]
        public async Task<IActionResult> GetDistribution()
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            var distribution = await _ratingService.GetRatingDistributionAsync(userId);
            return Ok(distribution);
        }

        [HttpGet("unique-raters-count")]
        public async Task<IActionResult> GetUniqueRatersCount()
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var count = await _ratingService.GetUniqueRatersCountAsync(userId);
            return Ok(new { count });
        }

        [HttpGet("check/{ratedUserId}")]
        public async Task<IActionResult> CheckExistingRating(string ratedUserId)
        {
            // Get userId from JWT claims
            var raterUserId = User.FindFirst("id")?.Value;
            if (raterUserId == null) return Unauthorized();

            var rating = await _ratingService.GetMostRecentRatingAsync(raterUserId, ratedUserId);
            if (rating == null)
                return Ok(new { message = "No rating found" });

            return Ok(rating);
        }


    }
}
