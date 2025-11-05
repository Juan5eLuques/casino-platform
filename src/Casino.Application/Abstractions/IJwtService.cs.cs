using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casino.Application.Abstractions;

public interface IJwtService
{
    string CreateToken(int id, string email, string role);
}