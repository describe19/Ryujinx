﻿namespace Ryujinx.HLE.HOS.Services.Hid
{
    // TODO: Refactor out to use HidControllerID and HidUtils conversion functions
    public enum HidNpadIdType
    {
        Player1  = 0,
        Player2  = 1,
        Player3  = 2,
        Player4  = 3,
        Player5  = 4,
        Player6  = 5,
        Player7  = 6,
        Player8  = 7,
        Unknown  = 16,
        Handheld = 32
    }
}