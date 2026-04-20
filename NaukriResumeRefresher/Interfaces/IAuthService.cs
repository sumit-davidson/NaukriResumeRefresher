using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaukriResumeRefresher.Interfaces
{
    public interface IAuthService
    {
        Task<HttpClient> LoginAsync();
    }
}
