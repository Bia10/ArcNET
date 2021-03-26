namespace ArcNET.Utilities
{
    public class HighResConfig
    {
        //Arcanum High Resolution Patch Settings
        //Basic:
        private int Width; // original: 800
        private int Height; // original: 600
        private int DialogFont; // 0 = size 12, 1 = size 14, 2 = size 18
        private int LogbookFont; // 0 = size 12, 1 = size 14
        private int MenuPosition; // 0 = top, 1 = center, 2 = bottom
        private int MainMenuArt; // 0 = black, 1 = fade to black, 2 = wood
        private int Borders; // 1 = add borders to most UI graphics
        private int Language; // 0 = English, 1 = German, 2 = French, 3 = Russian
        //Graphics:
        private int Windowed; // 0 = fullscreen mode, 1 = windowed mode
        private int Renderer; // 0 = software, 1 = hardware
        private int DoubleBuffer; // 0 = disabled, 1 = enabled (unless windowed)
        private int DDrawWrapper; // 1 = install DDrawCompat wrapper
        private int ShowFPS; // 0 = no change, 1 = always enabled
        //Advanced:
        private int ScrollFPS; // original: 35, max: 255
        private int ScrollDist; // original: 10, infinite: 0
        private int PreloadLimit; // original: 30 tiles, max: 255
        private int BroadcastLimit; // original: 10 tiles, max 255
        private int Logos; // 0 = skip Sierra/Troika logos
        private int Intro; // 0 = skip the main menu intro clip

        public HighResConfig()
        {
             Width = 2560; // original: 800
             Height = 1600; // original: 600
             DialogFont = 1; // 0 = size 12, 1 = size 14, 2 = size 18
             LogbookFont = 1; // 0 = size 12, 1 = size 14
             MenuPosition = 1; // 0 = top, 1 = center, 2 = bottom
             MainMenuArt = 1; // 0 = black, 1 = fade to black, 2 = wood
             Borders = 1; // 1 = add borders to most UI graphics
             Language = 0; // 0 = English, 1 = German, 2 = French, 3 = Russian
             Windowed = 0; // 0 = fullscreen mode, 1 = windowed mode
             Renderer = 0; // 0 = software, 1 = hardware
             DoubleBuffer = 0; // 0 = disabled, 1 = enabled (unless windowed)
             DDrawWrapper = 1; // 1 = install DDrawCompat wrapper
             ShowFPS = 1; // 0 = no change, 1 = always enabled
             ScrollFPS = 60; // original: 35, max: 255
             ScrollDist = 30; // original: 10, infinite: 0
             PreloadLimit = 60; // original: 30 tiles, max: 255
             BroadcastLimit = 20; // original: 10 tiles, max 255
             Logos = 0; // 0 = skip Sierra/Troika logos
             Intro = 0; // 0 = skip the main menu intro clip
        }
    }
}