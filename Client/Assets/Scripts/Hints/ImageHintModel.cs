﻿namespace CollaborationEngine.Hints
{
    public class ImageHintModel : HintModel
    {
        public ImageHintType ImageHintType { get; set; }

        public ImageHintModel()
        {
            Type = HintType.Image;
        }
    }
}
