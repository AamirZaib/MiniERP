using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{

    public interface IAiCommandService
    {
        Task<AiCommandProposal> ParseAsync(string command);
    }
}
