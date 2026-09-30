using System.Collections.Generic;

namespace VS_Mart_Backend.Features.Store
{
    public class CounterStatusStoreDto
    {
        public int Store_ID { get; set; }
        public string Store_Name { get; set; } = string.Empty;
    }

    public class CounterStatusDetailDto
    {
        public string Cash_Counter { get; set; } = string.Empty;
        public int STATUS { get; set; }
        public string LAST_UPDATED_DATE { get; set; } = string.Empty;
    }

    public class CounterStatusResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public IEnumerable<T>? Data { get; set; }
    }
}
