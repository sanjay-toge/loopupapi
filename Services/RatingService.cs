using LoopUpAPI.Models;
using MongoDB.Driver;
using LoopUpAPI.DTOs;
using MongoDB.Driver;
using MongoDB.Bson;

namespace LoopUpAPI.Services
{
    public class RatingService
    {
        private readonly IMongoCollection<Rating> _latestRatingsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Rating> _ratingsHistoryCollection;
        private readonly IMongoCollection<Rating> _pendingRatingsCollection;

        public RatingService(IMongoDatabase database)
        {
            _latestRatingsCollection = database.GetCollection<Rating>("ratings");
            _ratingsHistoryCollection = database.GetCollection<Rating>("history-ratings");
            _pendingRatingsCollection = database.GetCollection<Rating>("pending-ratings");
            _usersCollection = database.GetCollection<User>("users");
        }

        /// <summary>
        /// Add a rating request:
        /// - If relation = stranger → directly added to latest + history
        /// - Otherwise → goes to pending until accepted/rejected
        /// </summary>
        public async Task AddRatingRequestAsync(Rating rating)
        {
            if (rating.RaterUserId == rating.RatedUserId)
                throw new InvalidOperationException("You cannot rate yourself.");

            rating.CreatedAt = DateTime.UtcNow;
            rating.UpdatedAt = DateTime.UtcNow;

            if (rating.Relation?.ToLower() == "")
            {
                // Insert directly into history
                await _ratingsHistoryCollection.InsertOneAsync(rating);

                // Check if latest already exists
                var latestFilter = Builders<Rating>.Filter.And(
                    Builders<Rating>.Filter.Eq(r => r.RaterUserId, rating.RaterUserId),
                    Builders<Rating>.Filter.Eq(r => r.RatedUserId, rating.RatedUserId)
                );

                var existingLatest = await _latestRatingsCollection.Find(latestFilter).FirstOrDefaultAsync();

                if (existingLatest != null)
                {
                    var update = Builders<Rating>.Update
                        .Set(r => r.Score, rating.Score)
                        .Set(r => r.Comment, rating.Comment)
                        .Set(r => r.Relation, rating.Relation)
                        .Set(r => r.KnownSince, rating.KnownSince)
                        .Set(r => r.UpdatedAt, DateTime.UtcNow);

                    await _latestRatingsCollection.UpdateOneAsync(latestFilter, update);
                }
                else
                {
                    await _latestRatingsCollection.InsertOneAsync(rating);
                }

                // Update user's average rating immediately
                await UpdateUserAverageRating(rating.RatedUserId);
            }
            else
            {
                // Non-strangers go to pending first
                var filter = Builders<Rating>.Filter.And(
                            Builders<Rating>.Filter.Eq(r => r.RaterUserId, rating.RaterUserId),
                            Builders<Rating>.Filter.Eq(r => r.RatedUserId, rating.RatedUserId)
                        );

                var existingPending = await _pendingRatingsCollection.Find(filter).FirstOrDefaultAsync();

                if (existingPending != null)
                {
                    var update = Builders<Rating>.Update
                        .Set(r => r.Score, rating.Score)
                        .Set(r => r.Comment, rating.Comment)
                        .Set(r => r.Relation, rating.Relation)
                        .Set(r => r.KnownSince, rating.KnownSince)
                        .Set(r => r.UpdatedAt, DateTime.UtcNow);

                    await _pendingRatingsCollection.UpdateOneAsync(filter, update);
                }
                else
                {
                    await _pendingRatingsCollection.InsertOneAsync(rating);
                }
            }
        }


        /// <summary>
        /// Accept a pending rating, move it into latest + history.
        /// </summary>
        public async Task AcceptRatingAsync(string pendingRatingId)
        {
            var filter = Builders<Rating>.Filter.Eq(r => r.Id, pendingRatingId);
            var pending = await _pendingRatingsCollection.Find(filter).FirstOrDefaultAsync();

            if (pending == null)
                throw new InvalidOperationException("Pending rating not found.");

            // Always insert into history
            var ratingHistory = new Rating
            {
                RatedUserId = pending.RatedUserId,
                RaterUserId = pending.RaterUserId,
                Score = pending.Score,
                Comment = pending.Comment,
                Relation = pending.Relation,
                KnownSince = pending.KnownSince,
                CreatedAt = pending.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            await _ratingsHistoryCollection.InsertOneAsync(ratingHistory);

            // Check if latest already exists
            var latestFilter = Builders<Rating>.Filter.And(
                Builders<Rating>.Filter.Eq(r => r.RaterUserId, pending.RaterUserId),
                Builders<Rating>.Filter.Eq(r => r.RatedUserId, pending.RatedUserId)
            );

            var existingLatest = await _latestRatingsCollection.Find(latestFilter).FirstOrDefaultAsync();

            if (existingLatest != null)
            {
                var update = Builders<Rating>.Update
                    .Set(r => r.Score, pending.Score)
                    .Set(r => r.Comment, pending.Comment)
                    .Set(r => r.Relation, pending.Relation)
                    .Set(r => r.KnownSince, pending.KnownSince)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow);

                await _latestRatingsCollection.UpdateOneAsync(latestFilter, update);
            }
            else
            {
                pending.CreatedAt = DateTime.UtcNow;
                pending.UpdatedAt = DateTime.UtcNow;
                await _latestRatingsCollection.InsertOneAsync(pending);
            }

            // Remove from pending
            await _pendingRatingsCollection.DeleteOneAsync(filter);

            // Update user rating
            await UpdateUserAverageRating(pending.RatedUserId);
        }

        /// <summary>
        /// Reject a pending rating (just remove it).
        /// </summary>
        public async Task RejectRatingAsync(string pendingRatingId)
        {
            await _pendingRatingsCollection.DeleteOneAsync(r => r.Id == pendingRatingId);
        }

        /// <summary>
        /// Get confirmed ratings for a user.
        /// </summary>
        public async Task<List<Rating>> GetRatingsForUserAsync(string userId)
        {
            return await _latestRatingsCollection
                .Find(r => r.RatedUserId == userId)
                .ToListAsync();
        }

        private async Task UpdateUserAverageRating(string userId)
        {
            var ratings = await _latestRatingsCollection
                .Find(r => r.RatedUserId == userId)
                .ToListAsync();

            if (!ratings.Any())
            {
                await _usersCollection.UpdateOneAsync(
                    u => u.Id == userId,
                    Builders<User>.Update.Set(u => u.Rating, 0)
                );
                return;
            }

            double totalScore = 0;
            double totalWeight = 0;

            foreach (var rating in ratings)
            {
                double weight = GetWeightForRating(rating);
                totalScore += rating.Score * weight;
                totalWeight += weight;
            }

            var weightedAverage = totalWeight > 0 ? totalScore / totalWeight : 0;

            await _usersCollection.UpdateOneAsync(
                u => u.Id == userId,
                Builders<User>.Update.Set(u => u.Rating, (float)Math.Round(weightedAverage, 2))
            );
        }

        private double GetWeightForRating(Rating rating)
        {
            double weight = 0.3; // Default for strangers

            switch (rating.Relation?.ToLower())
            {
                case "friend":
                    weight = 1.0;
                    break;
                case "acquaintance":
                    weight = 0.6;
                    break;
                case "stranger":
                default:
                    weight = 0.3;
                    break;
            }

            if (rating.KnownSince.HasValue && rating.KnownSince > 12)
            {
                weight += 0.1;
            }

            return Math.Min(weight, 1.2);
        }
        /// <summary>
        /// Get all pending ratings for a specific user (waiting for them to accept/reject).
        /// </summary>
        public async Task<List<Rating>> GetPendingRatingsForUserAsync(string userId)
        {
            return await _pendingRatingsCollection
                .Find(r => r.RatedUserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Get recent ratings given by a specific user (who they rated).
        /// </summary>
        public async Task<List<RecentlyRatedDto>> GetRecentlyRatedUsersAsync(string raterUserId, int limit = 10)
        {
            // Get from latest ratings
            var latest = await _latestRatingsCollection
                .Find(r => r.RaterUserId == raterUserId)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Get from pending ratings
            var pending = await _pendingRatingsCollection
                .Find(r => r.RaterUserId == raterUserId)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Combine
            var combined = latest.Concat(pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Pick unique by RatedUserId
            var uniqueByRatedUser = combined
                .GroupBy(r => r.RatedUserId)
                .Select(g => g.First())
                .Take(limit)
                .ToList();

            // Fetch user details only for unique RatedUserIds
            var ratedUserIds = uniqueByRatedUser.Select(r => r.RatedUserId).ToList();
            var users = await _usersCollection.Find(u => ratedUserIds.Contains(u.Id)).ToListAsync();

            // Merge rating + user info
            var result = (from rating in uniqueByRatedUser
                          join user in users on rating.RatedUserId equals user.Id
                          select new RecentlyRatedDto
                          {
                              RatingId = rating.Id!,
                              RatedUserId = user.Id!,
                              Name = user.Name,
                              Image = user.Image,
                              CreatedAt = rating.CreatedAt
                          }).ToList();

            return result;
        }


        // ✅ Count of people I have rated
        public async Task<long> GetCountOfPeopleIRatedAsync(string raterUserId)
        {
            var latestCount = await _latestRatingsCollection
              .Find(r => r.RaterUserId == raterUserId)
              .CountDocumentsAsync();

            var pendingCount = await _pendingRatingsCollection
                .Find(r => r.RaterUserId == raterUserId)
                .CountDocumentsAsync();

            return latestCount + pendingCount;
        }

        // ✅ Count of mutual ratings
        public async Task<long> GetMutualRatingsCountAsync(string userId)
        {
            var userRatedIds = await _latestRatingsCollection
                .Find(r => r.RaterUserId == userId)
                .Project(r => r.RatedUserId)
                .ToListAsync();

            return await _latestRatingsCollection
                .Find(r => userRatedIds.Contains(r.RaterUserId) && r.RatedUserId == userId)
                .CountDocumentsAsync();
        }

        // ✅ Rating distribution (how many 1,2,3,4,5)
        public async Task<List<RatingDistributionDto>> GetRatingDistributionAsync(string userId)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("RaterUserId", userId)),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$Score" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var result = await _latestRatingsCollection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            return result.Select(doc => new RatingDistributionDto
            {
                Score = doc["_id"].AsInt32,
                Count = doc["count"].AsInt32
            }).ToList();
        }

        public async Task<long> GetUniqueRatersCountAsync(string userId)
        {
            var filter = Builders<Rating>.Filter.Eq(r => r.RatedUserId, userId);

            // Use string field name "RaterUserId" instead of lambda
            var distinctRaters = await _latestRatingsCollection
                .Distinct<string>("RaterUserId", filter)
                .ToListAsync();

            return distinctRaters.Count;
        }

        public async Task<Rating?> GetMostRecentRatingAsync(string raterUserId, string ratedUserId)
        {
            // Fetch pending rating
            var pendingFilter = Builders<Rating>.Filter.And(
                Builders<Rating>.Filter.Eq(r => r.RaterUserId, raterUserId),
                Builders<Rating>.Filter.Eq(r => r.RatedUserId, ratedUserId)
            );
            var pending = await _pendingRatingsCollection.Find(pendingFilter).FirstOrDefaultAsync();

            // Fetch latest rating
            var latestFilter = Builders<Rating>.Filter.And(
                Builders<Rating>.Filter.Eq(r => r.RaterUserId, raterUserId),
                Builders<Rating>.Filter.Eq(r => r.RatedUserId, ratedUserId)
            );
            var latest = await _latestRatingsCollection.Find(latestFilter).FirstOrDefaultAsync();

            // Determine the latest timestamp for each
            DateTime? pendingTime = pending != null ? (pending.UpdatedAt > pending.CreatedAt ? pending.UpdatedAt : pending.CreatedAt) : null;
            DateTime? latestTime = latest != null ? (latest.UpdatedAt > latest.CreatedAt ? latest.UpdatedAt : latest.CreatedAt) : null;

            // Compare and return the most recent
            if (pendingTime.HasValue && latestTime.HasValue)
            {
                return pendingTime > latestTime ? pending : latest;
            }

            return pending ?? latest;
        }



    }

}
