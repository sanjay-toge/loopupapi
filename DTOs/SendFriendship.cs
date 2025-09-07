namespace LoopUpAPI.DTOs
{
    public class SendFriendship
    {
        // The ID of the user you want to send a request to
        public string RecipientId { get; set; } = string.Empty;
    }
}
