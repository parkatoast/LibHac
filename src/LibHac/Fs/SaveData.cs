﻿using LibHac.Ncm;

namespace LibHac.Fs;

public static class SaveData
{
    public static readonly ulong SaveIndexerId = 0x8000000000000000;
    public static ProgramId InvalidProgramId => ProgramId.InvalidId;
    public static ProgramId AutoResolveCallerProgramId => new ProgramId(ulong.MaxValue - 1);
    public static UserId InvalidUserId => UserId.InvalidId;
    public static ulong InvalidSystemSaveDataId => 0;
}
