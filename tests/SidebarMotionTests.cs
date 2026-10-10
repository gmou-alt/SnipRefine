using System;

namespace SnapGrab
{
    internal static class SidebarMotionTests
    {
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

        internal static void Run()
        {
            SidebarMotion motion = new SidebarMotion();
            motion.Jump(0);
            motion.MoveTo(1);
            double previous = 0;
            for (int i = 0; i < 60; i++)
            {
                double position = motion.Advance(1.0 / 60);
                Check(position >= previous && position <= 1, "Opening overshoot or backwards motion");
                previous = position;
            }
            Check(motion.Position == 1 && !motion.Moving, "Opening must settle at exact target");
            Console.WriteLine("PASS: open bounds and settling");

            motion.MoveTo(0);
            for (int i = 0; i < 60; i++) motion.Advance(1.0 / 60);
            Check(motion.Position == 0 && !motion.Moving, "Closing must settle");
            Console.WriteLine("PASS: close settling");

            motion.MoveTo(1);
            motion.Advance(0.08);
            double before = motion.Position, velocity = motion.Velocity;
            motion.MoveTo(0);
            Check(motion.Position == before && motion.Velocity == velocity, "Retarget must preserve position and velocity");
            for (int i = 0; i < 60; i++) motion.Advance(1.0 / 60);
            Check(motion.Position == 0, "Reversal must settle");
            Console.WriteLine("PASS: continuous reversal");

            SidebarMotion sixty = new SidebarMotion(), thirty = new SidebarMotion();
            sixty.MoveTo(1); thirty.MoveTo(1);
            for (int i = 0; i < 12; i++) sixty.Advance(1.0 / 60);
            for (int i = 0; i < 6; i++) thirty.Advance(1.0 / 30);
            Check(Math.Abs(sixty.Position - thirty.Position) < 1e-10, "Position depends on frame rate");
            Console.WriteLine("PASS: frame-rate independence");

            for (int i = 0; i < 100; i++)
            {
                motion.MoveTo(i % 2);
                motion.Advance(i % 3 == 0 ? 0.08 : 0.012);
                Check(motion.Position >= 0 && motion.Position <= 1, "Rapid toggle left valid range");
            }
            motion.MoveTo(1); motion.Advance(2);
            Check(motion.Position == 1 && !motion.Moving, "Delayed timer must settle without instability");
            Console.WriteLine("PASS: rapid toggles and delayed frame");

            double saved = motion.Position;
            motion.Advance(double.NaN); motion.Advance(double.PositiveInfinity); motion.Advance(-1);
            Check(motion.Position == saved, "Invalid elapsed time corrupted position");
            Console.WriteLine("PASS: invalid elapsed time");

            motion.Jump(0);
            Check(motion.Position == 0 && motion.Velocity == 0 && !motion.Moving, "Reduced-motion jump must be immediate");
            Console.WriteLine("PASS: immediate jump");
        }

        public static int Main()
        {
            try { Run(); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
