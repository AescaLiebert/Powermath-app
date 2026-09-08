using System;
using System.Collections;

namespace PowerMath.Session
{
    // Future transport contract. A direct Firestore/client-clock implementation must
    // never claim this capability or enable protected live rewards.
    public interface IServerGameCommands
    {
        IEnumerator FetchRelease(Action<ServerReleasePolicy> succeeded, Action<string> failed);
        IEnumerator ClaimMail(MailClaim command, Action<ServerCommandReceipt> succeeded, Action<string> failed);
        IEnumerator EnterEvent(EventEntry command, Action<ServerCommandReceipt> succeeded, Action<string> failed);
        IEnumerator AcknowledgeAnnouncement(AnnouncementRead command, Action<ServerCommandReceipt> succeeded, Action<string> failed);
    }

    [Serializable] public sealed class ServerReleasePolicy
    {
        public string releaseId;
        public int protocolVersion;
        public long serverUnixSeconds;
        public bool maintenance;
        public bool mailboxEnabled;
        public bool eventsEnabled;
    }
    [Serializable] public sealed class MailClaim
    {
        public string operationId;
        public string mailId;
        public long expectedRevision;
    }
    [Serializable] public sealed class EventEntry
    {
        public string operationId;
        public string eventId;
        public string catalogVersion;
        public long expectedRevision;
    }
    [Serializable] public sealed class AnnouncementRead
    {
        public string operationId;
        public string announcementId;
        public string revision;
    }
    [Serializable] public sealed class ServerCommandReceipt
    {
        public string operationId;
        public string resourceId;
        public long revision;
        public long committedAtUnixSeconds;
        public PlayerData.PlayerSnapshot player;
    }
}
