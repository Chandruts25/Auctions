using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarReview = Agape.Auctions.Models.Cars.CarReview;

namespace Agape.Auctions.UI.Cars.Models
{
    public class CarReviewDetails : CarReview
    {
        public List<CarReviewDetails> ReplyReviewDetails { get; set; }
    }
}
