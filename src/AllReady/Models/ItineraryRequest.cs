﻿using System;
using System.ComponentModel.DataAnnotations;

namespace AllReady.Models
{
    /// <summary>
    /// Represents which requests are attached to which itinerary
    /// </summary>
    public class ItineraryRequest
    {
        [Required]
        public int ItineraryId { get; set; }
        public Itinerary Itinerary { get; set; }

        [Required]
        public Guid RequestId { get; set; }
        public Request Request { get; set; }

        [Required]
        public DateTime DateAssigned { get; set; }

        [Required]
        public int OrderIndex { get; set; }
    }
}
