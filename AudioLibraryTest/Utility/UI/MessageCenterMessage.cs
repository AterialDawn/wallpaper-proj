namespace player.Utility.UI
{
    class MessageCenterMessage
    {
        public string Message { get; }
        public float Alpha { get; private set; } = 1f;
        public bool Alive { get; private set; } = true;

        private double fadeTime;
        private double lifeTime;

        internal MessageCenterMessage(string message, double fadeTime, double lifeTime)
        {
            Message = message;
            this.fadeTime = fadeTime;
            this.lifeTime = lifeTime;
        }

        public void Update(double time)
        {
            lifeTime -= time;
            if (lifeTime <= 0)
            {
                Alive = false;
                Alpha = 0;
                return;
            }

            if (lifeTime < fadeTime)
            {
                //begin fading
                double fadeElapsedTime = fadeTime - lifeTime;
                Alpha = 1f - (float)(fadeElapsedTime / fadeTime);
            }
        }
    }
}
