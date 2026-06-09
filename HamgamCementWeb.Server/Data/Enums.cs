using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data
{
    public enum PersonType
    {
        [Display(Name = "حقیقی")]
        NaturalPerson = 1,

        [Display(Name = "حقوقی")]
        LegalEntity = 2
    }
    public enum PersonTitle
    {
        [Display(Name = "آقا")]
        Mr,

        [Display(Name = "خانم")]
        Mrs,
    }

}
