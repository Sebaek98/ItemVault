using System;

namespace ItemVault;

[Serializable]
public class LockEntry
{
    public string ListName { get; set; } = "Locked";
    public string Note { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
}
