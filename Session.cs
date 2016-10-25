namespace VerySimple
{
    using System;
    
    public class Session
    {
        public string SessionId { get; set; }
        public int Size { get; set; }
        public byte[] Value { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public TimeSpan? Lifetime { get; set; }
        public bool IsSlidingExpiry { get; set; }
    }
}