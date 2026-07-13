// namespace ShiftedSignal.Garden.Units
// {
//     public enum owner
//     {
//         Invalid = 0,
//         Player1 = 1,
//         AI1  = 2,
//         AI2 = 4,
//         AI3 = 8,
//         AI4 = 16,
//         AI5 = 32,
//         AI6 = 64,
//         AI7 = 128,
//         Unowned = 256
//     }
// }


    public enum Owner
    {
        Invalid = 0,
        Player = 1,
        Friendly = 2,
        Enemy = 4,
        Buildable = 8,
        Unowned = 16,
    }