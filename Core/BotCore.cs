﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    internal class BotCore
    {
        MoveGenerator _moveGenerator;
        public BotCore()
        {
            _moveGenerator = new MoveGenerator();
        }
    }
}