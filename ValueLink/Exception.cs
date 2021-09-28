// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace ValueLink
{
    public class UnmatchedGoshujinException : System.Exception
    {
        private const string ExceptionMessage = "This object is the property of different Goshujin-sama.";

        public UnmatchedGoshujinException()
            : base(ExceptionMessage)
        {
        }
    }
}
