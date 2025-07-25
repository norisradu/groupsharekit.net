﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Sdl.Community.GroupShareKit.Clients.Activities
{
    public class Filter
    {
        [JsonProperty("activitySources")]
        public List<string> ActivitySources { get; set; }

        [JsonProperty("users")]
        public List<Guid> Users { get; set; }

        [JsonProperty("lastUsedStart")]
        public DateTime? LastUsedStart { get; set; }

        [JsonProperty("lastUsedEnd")]
        public DateTime? LastUsedEnd { get; set; }

        [JsonProperty("showOnlineOnly")]
        public bool? ShowOnlineOnly { get; set; }

        [JsonProperty("searchText")]
        public string SearchText { get; set; }

        public string SerializeFilter()
        {
            var serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ" };
            return JsonConvert.SerializeObject(this, serializerSettings);
        }
    }
}
