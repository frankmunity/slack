﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tab.Slack.Common.Model.Responses
{
    public class EmojiResponse : ResponseBase
    {
        public Dictionary<string, string> Emoji { get; set; }
    }
}
