using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class FacebookUserData
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public FacebookPicture? Picture { get; set; }
    }
    public class FacebookPicture
    {
        public FacebookPictureData? Data { get; set; }
    }

    public class FacebookPictureData
    {
        public string Url { get; set; } = string.Empty;
    }
}
