namespace SnapGrab
{
    internal class AiPreset
    {
        public string Protocol = "openai";

        public string[] Models;

        public string Name;

        public string Endpoint;

        public string Model;

        public string Hint;

        public string ImagesUrl;

        public string ImagesModel;

        public AiPreset(string name, string endpoint, string model, string hint)
            : this(name, endpoint, model, hint, null, null)
        {
        }

        public AiPreset(string name, string endpoint, string model, string hint, string imagesUrl, string imagesModel)
        {
            Name = name;
            Endpoint = endpoint;
            Model = model;
            Models = string.IsNullOrEmpty(model) ? new string[0] : new string[] { model };
            Hint = hint;
            ImagesUrl = imagesUrl;
            ImagesModel = imagesModel;
        }
        public AiPreset(string name, string endpoint, string protocol, string[] models, string hint)
            : this(name, endpoint, models.Length > 0 ? models[0] : "", hint)
        {
            Protocol = protocol;
            Models = (string[])models.Clone();
        }

        public void Fill(AiProvider provider)
        {
            // Never carry credentials to a different service or subscription endpoint.
            if (!string.IsNullOrEmpty(Endpoint))
            {
                if (!string.Equals(provider.Endpoint, Endpoint, System.StringComparison.OrdinalIgnoreCase))
                    provider.ApiKey = "";
                provider.Endpoint = Endpoint;
            }
            provider.Name = Name;
            provider.Protocol = Protocol;
            if (Models.Length > 0)
            {
                provider.Models = new System.Collections.Generic.List<string>(Models);
                provider.SelectedModel = Model;
            }
        }
    }
}
