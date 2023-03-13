using MongoDB.Bson.Serialization.Attributes;

namespace VC.BinanceSupporter.Model
{
    public class TeleUser
    {
        /// <summary>
        /// Unique identifier for this user or bot
        /// </summary>
        [BsonElement("teleid")]
        public long TeleId { get; set; }

        /// <summary>
        /// <see langword="true"/>, if this user is a bot
        /// </summary>
        [BsonElement("isbot")]
        public bool IsBot { get; set; }

        /// <summary>
        /// User's or bot’s first name
        /// </summary>
        [BsonElement("firstName")]
        public string FirstName { get; set; } = default!;

        /// <summary>
        /// Optional. User's or bot’s last name
        /// </summary>
        [BsonElement("lastName")]
        public string? LastName { get; set; }

        /// <summary>
        /// Optional. User's or bot’s username
        /// </summary>
        [BsonElement("username")]
        public string? Username { get; set; }

        /// <summary>
        /// Optional. <a href="https://en.wikipedia.org/wiki/IETF_language_tag">IETF language tag</a> of the
        /// user's language
        /// </summary>
        [BsonElement("languageCode")]
        public string? LanguageCode { get; set; }

        /// <summary>
        /// Optional. <see langword="true"/>, if this user is a Telegram Premium user
        /// </summary>
        [BsonElement("isPremium")]
        public bool? IsPremium { get; set; }

        /// <summary>
        /// Optional. <see langword="true"/>, if this user added the bot to the attachment menu
        /// </summary>
        [BsonElement("addedToAttachmentMenu")]
        public bool? AddedToAttachmentMenu { get; set; }

        /// <summary>
        /// Optional. <see langword="true"/>, if the bot can be invited to groups. Returned only in <see cref="Requests.GetMeRequest"/>
        /// </summary>
        [BsonElement("canJoinGroups")]
        public bool? CanJoinGroups { get; set; }

        /// <summary>
        /// Optional. <see langword="true"/>, if privacy mode is disabled for the bot. Returned only in <see cref="Requests.GetMeRequest"/>
        /// </summary>
        [BsonElement("canReadAllGroupMessages")]
        public bool? CanReadAllGroupMessages { get; set; }

        /// <summary>
        /// Optional. <see langword="true"/>, if the bot supports inline queries. Returned only in <see cref="Requests.GetMeRequest"/>
        /// </summary>
        [BsonElement("supportsInlineQueries")]
        public bool? SupportsInlineQueries { get; set; }
    }
}
