﻿namespace IDKEngine
{
    class Program
    {
        private static unsafe void Main()
        {
            Application application = new Application(1280, 720, "IDKEngine");
            application.Start();
        }
    }
}
