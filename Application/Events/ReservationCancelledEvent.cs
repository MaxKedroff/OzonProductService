using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Events
{
    public class ReservationsCancelledEvent
    {
        public Guid OrderId { get; set; }

        public List<OrderCancelledItem> Items { get; set; } = new();
        public DateTime CancelledAt { get; set; }

        public string? Reason { get; set; }

        public bool IsComplete { get; set; } = true;

        public List<string>? Errors { get; set; }
    }
}
