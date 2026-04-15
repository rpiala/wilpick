using System.ComponentModel.DataAnnotations;

namespace WilPick.ViewModels
{
    public class CoWpViewModel
    {

        public int cw_id { get; set; }

        public int co_id { get; set; }

        public int wp_id { get; set; }

        public string? cw_code_name { get; set; }

        public DateTime? from_date { get; set; }

        public DateTime? thru_date { get; set; }

        public string? rec_status { get; set; }

        public string? print_status { get; set; }

        public decimal? prize { get; set; }

        public decimal? commission { get; set; }

        public string? app_password { get; set; }

        public string? bet_type { get; set; }

        public decimal? load_balance { get; set; }
    }


    public class CoWpNosViewModel
    {
        public int Cwn_id { get; set; }

        public int Cw_id { get; set; }

        public int Co_id { get; set; }

        public int Wp_id { get; set; }

        public string Mobile_no { get; set; } = string.Empty;

        public string? Assign_name { get; set; }

        public string? Trans_status { get; set; }

        public DateTime? Date_started { get; set; }

        public string? Res_col { get; set; }

        public string? Smpp_op { get; set; }

        public string? Fb_id { get; set; }

        public string? App_password { get; set; }

        public decimal? Prize { get; set; }

        public decimal? Commission { get; set; }

        public string? Bet_type { get; set; }

        public decimal? Load_balance { get; set; }
    }


    public class SwCoSwEntryViewModel
    {
        public string cse_no { get; set; } = string.Empty;

        public string cbd_dtl_no { get; set; } = string.Empty;

        public string cbh_no { get; set; } = string.Empty;

        public int user_id { get; set; }

        public int cw_id { get; set; }

        public int co_id { get; set; }

        public int wp_id { get; set; }

        public DateTime? draw_sked { get; set; }

        public int? batch_id { get; set; }

        public string? cvm_no { get; set; }

        public DateTime? entry_date { get; set; }

        public string? cse_combination { get; set; }

        public decimal? cse_bet { get; set; }

        public int? batch_printing { get; set; }
    }

    public class SwCoBetDtlViewModel
    {
        public int? RowNum { get; set; }
        public string cbd_dtl_no { get; set; } = string.Empty;
        public string? cbd_dtl_no_enc { get; set; }

        public string cbh_no { get; set; } = string.Empty;

        public int user_id { get; set; }

        public int cw_id { get; set; }

        public int co_id { get; set; }

        public int wp_id { get; set; }

        public int? batch_id { get; set; }

        public string? cvm_no { get; set; }
        [Display(Name = "Combination")]
        [Required]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "Must be exactly 3 digits.")]

        public string? cbd_msg { get; set; }
        public string? prev_cbd_msg { get; set; }
        public string cbd_bet
        {
            get
            {
                var t = target ?? 0;
                var r = ramble ?? 0;

                if (t > 0 && r == 0)
                    return t.ToString();

                if (t > 0 && r > 0)
                    return $"{t}R{r}";

                if (t <= 0 && r > 0)
                    return $"R{r}";

                return string.Empty;
            }
        }

        public string? prev_cbd_bet { get; set; }

        public int? target { get; set; }

        public int? ramble { get; set; }

        public string? status { get; set; }

        public DateTime? draw_sked { get; set; }

        public DateTime? entry_date { get; set; }

        public decimal? aser_commission { get; set; }

        public decimal? aser_prize { get; set; }

        public decimal? co_commission { get; set; }

        public decimal? co_prize { get; set; }

        public int? divider { get; set; }

        public string? bet_type { get; set; }

        public int? print_status { get; set; }

        public string? print_trans_no { get; set; }
        public IList<SwCoSwEntryViewModel> SwEntries { get; set; }
        public string? PlayerName { get; set; }        
        public decimal? LoadBalance { get; set; }
        public decimal? WinningPrize { get; set; }
    }

    public class SwCoBetHdrViewModel
    {
        public string cbh_no { get; set; } = string.Empty;
        public string? cbh_no_enc { get; set; }

        public int user_id { get; set; }

        public int cw_id { get; set; }

        public int co_id { get; set; }

        public int wp_id { get; set; }

        public int? batch_id { get; set; }

        public string? cvm_no { get; set; }

        public DateTime? draw_sked { get; set; }

        public DateTime? date_encoded { get; set; }
        public bool IsCuttOff { get; set; }
        public decimal? PlayerRemainingload { get; set; }
        public string? BetType { get; set; }
        public decimal? TotalBetAmount { get; set; }
        public decimal? TotalCashIn { get; set; }
        public decimal? OverallTotalBet { get; set; }
        public decimal? TotalCashOut { get; set; }
        public IList<SwCoBetDtlViewModel> BetDetails { get; set; }
    }


}