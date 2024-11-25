using System.Collections.Generic;

public class Response
{
    public string id { get; set; }
    public int status { get; set; }
    public Result result { get; set; }
    public RateLimits[] rateLimits { get; set; }
}

public class Result
{
    public long lastUpdateId { get; set; }
    public List<List<string>> bids { get; set; } // Список списков строк для Bid
    public List<List<string>> asks { get; set; } // Список списков строк для Ask
}

public class RateLimits
{
    public string rateLimitType { get; set; }
    public string interval { get; set; }
    public int intervalNum { get; set; }
    public int limit { get; set; }
    public int count { get; set; }
}