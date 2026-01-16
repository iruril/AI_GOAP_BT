public interface IChatSender
{
    string Nickname { get; }
    Team MyTeam { get; }
    uint NetId { get; }
}