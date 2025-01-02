using MonsterCardGame.Server.HttpModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zelenay_MTCG.Server.Endpoints
{
    internal interface IEndpoint
    {
        void HandleRequest(Request request, Response response);
    }
}
