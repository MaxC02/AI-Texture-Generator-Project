using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

public class StablePluginUI : EditorWindow
{

    // Create a UI that can be found under Tools/StablePluginUI
    [MenuItem("Tools/StablePluginUI")]
    public static void ShowWindow()
    {
        var window = GetWindow<StablePluginUI>();

        window.titleContent = new GUIContent("StablePluginUI");
        window.minSize = new Vector2(512, 760);

        LoadPreferences();
    }

    static string url = "http://127.0.0.1:9090/";                                                                           // Localhost directory
    static string directory = @"C:\Users\Max\Downloads\InvokeAI-release-1.14.1\";                                           // Stable Diffusion directory
    static string prefix = "StablePluginUI_";
    static string imagePath = null;
    static Texture2D resultsTexture;
    string[] sizeOptions = { "32", "64", "128", "256", "512", "1024" };
    int[] sizeOptionsInt = { 32, 64, 128, 256, 512, 1024 };

    string[] samplerOptions = { "ddim", "plms", "k_lms", "k_dpm_2", "k_dpm_2_a", "k_euler", "k_euler_a", "k_heun" };


    static string prompt = "Empty";                                                                                     // The parameters that are sent to Stable Diffusion

    static int iterations = 1;

    static int steps = 50;

    static float cfgScale = 7.5f;

    static int samplerIndex = 2;

    static int sizeIndex = 4;

    static string seed = "-1";

    static bool seamless = false;

    static float variationAmount = 0;

    static string withVariations = "";

    static Object initImg = null;

    static float strength = 0.75f;

    static bool fit = true;

    static float gfpganStrength = 0.8f;

    static string upscaleLevel = "";

    static float upscaleStrength = 0.75f;

    static string initImgName = "";

    // Create a UI window for the plugin
    void OnGUI()
    {
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(40));

        if (GUILayout.Button("Generate", GUILayout.Height(44)))
        {
            Generate();
        }
        steps = EditorGUILayout.IntField("Steps", steps);
        sizeIndex = EditorGUILayout.Popup("Size", sizeIndex, sizeOptions);
        samplerIndex = EditorGUILayout.Popup("Sampler", samplerIndex, samplerOptions);
        seed = EditorGUILayout.TextField("Seed", seed);
        seamless = EditorGUILayout.Toggle("Seamless", seamless);

        // Results
        EditorGUILayout.LabelField(new GUIContent(resultsTexture), GUILayout.Width(EditorGUIUtility.currentViewWidth), GUILayout.Height(EditorGUIUtility.currentViewWidth));

        if (GUILayout.Button("Import", GUILayout.Height(34)))
        {
            ImportTexture();
        }
    }

    void ImportTexture()
    {
        // Copy the generated image to Assets folder
        if (File.Exists(imagePath))
        {
            if (!Directory.Exists("Assets/Textures"))                                           // Searches for Texture folder in Assets, if it doesn't exist, create it
            {
                Directory.CreateDirectory("Assets/Textures");
            }
            File.Copy(imagePath, "Assets/Textures/" + Path.GetFileName(imagePath), true);
            AssetDatabase.Refresh();
        }
    }

    void Generate()                                                                                         // Class that sends the data to the Stable Diffusion build
    {
        imagePath = null;

        SavePreferences();

        string fitStr = fit ? "on" : "off";                                                                   // Send parameters to Stable Diffusion via HTTP POST
        string seamlessStr = seamless ? "'seamless': 'on'," : "";
        int width = sizeOptionsInt[sizeIndex];
        string samplerName = samplerOptions[samplerIndex];
        int height = width;
        var initImgObj = JsonToC.Serialize(initImg);
        string postData = $"{{'prompt':'{prompt}','iterations':'{iterations}','steps':'{steps}','cfg_scale':'{cfgScale}','sampler_name':'{samplerName}','width':'{width}'," +
            $"'height':'{height}'," + seamlessStr + $"'seed':'{seed}','variation_amount':'{variationAmount}','with_variations':'{withVariations}','initimg':{initImgObj}," +
            $"'strength':'{strength}','fit':'{fitStr}','gfpgan_strength':'{gfpganStrength}','upscale_level':'{upscaleLevel}','upscale_strength':'{upscaleStrength}'," +
            $"'initimg_name':'{initImgName}'}}";
        postData = postData.Replace("'", "\"");
        var request = (HttpWebRequest)WebRequest.Create(url);
        var data = Encoding.ASCII.GetBytes(postData);

        request.KeepAlive = true;
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        var response = (HttpWebResponse)request.GetResponse();
        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        var jsonRows = responseString.Split('\n');
        var lastRow = jsonRows[jsonRows.Length - 2];
        var deserialize = JsonToC.Deserialize<RootGetSet>(lastRow);
        imagePath = Path.Combine(directory, deserialize.url);
        resultsTexture = new Texture2D(2, 2);
        ImageConversion.LoadImage(resultsTexture, File.ReadAllBytes(imagePath));
    }

    static void LoadPreferences()                        // Class to load a set of preferences
    {
        prompt = EditorPrefs.GetString(prefix + "prompt", prompt);
        iterations = EditorPrefs.GetInt(prefix + "iterations", iterations);
        steps = EditorPrefs.GetInt(prefix + "steps", steps);
        cfgScale = EditorPrefs.GetFloat(prefix + "cfg_scale", cfgScale);
        sizeIndex = EditorPrefs.GetInt(prefix + "sizeIndex", sizeIndex);
        samplerIndex = EditorPrefs.GetInt(prefix + "samplerIndex", samplerIndex);
        seed = EditorPrefs.GetString(prefix + "seed", seed);
        seamless = EditorPrefs.GetBool(prefix + "seamless", seamless);
        variationAmount = EditorPrefs.GetFloat(prefix + "variation_amount", variationAmount);
        withVariations = EditorPrefs.GetString(prefix + "with_variations", withVariations);
        initImgName = EditorPrefs.GetString(prefix + "initimg_name", initImgName);
        strength = EditorPrefs.GetFloat(prefix + "strength", strength);
        fit = EditorPrefs.GetBool(prefix + "fit", fit);
        gfpganStrength = EditorPrefs.GetFloat(prefix + "gfpgan_strength", gfpganStrength);
        upscaleLevel = EditorPrefs.GetString(prefix + "upscale_level", upscaleLevel);
        upscaleStrength = EditorPrefs.GetFloat(prefix + "upscale_strength", upscaleStrength);
    }

    void SavePreferences()              // Class to save a set of preferences
    {
        EditorPrefs.SetString(prefix + "prompt", prompt);
        EditorPrefs.SetInt(prefix + "iterations", iterations);
        EditorPrefs.SetInt(prefix + "steps", steps);
        EditorPrefs.SetFloat(prefix + "cfg_scale", cfgScale);
        EditorPrefs.SetInt(prefix + "sizeIndex", sizeIndex);
        EditorPrefs.SetInt(prefix + "samplerIndex", samplerIndex);
        EditorPrefs.SetString(prefix + "seed", seed);
        EditorPrefs.SetBool(prefix + "seamless", seamless);
        EditorPrefs.SetFloat(prefix + "variation_amount", variationAmount);
        EditorPrefs.SetString(prefix + "with_variations", withVariations);
        EditorPrefs.SetString(prefix + "initimg_name", initImgName);
        EditorPrefs.SetFloat(prefix + "strength", strength);
        EditorPrefs.SetBool(prefix + "fit", fit);
        EditorPrefs.SetFloat(prefix + "gfpgan_strength", gfpganStrength);
        EditorPrefs.SetString(prefix + "upscale_level", upscaleLevel);
        EditorPrefs.SetFloat(prefix + "upscale_strength", upscaleStrength);
    }

    public class GetSet              // Getters and setters for the parameters
    {
        public string prompt { get; set; }
        public string iterations { get; set; }
        public string steps { get; set; }
        public string cfg_scale { get; set; }
        public string sampler_name { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public long seed { get; set; }
        public string variation_amount { get; set; }
        public string with_variations { get; set; }
        public string initimg { get; set; }
        public string strength { get; set; }
        public string fit { get; set; }
        public string gfpgan_strength { get; set; }
        public string upscale_level { get; set; }
        public string upscale_strength { get; set; }
    }

    public class RootGetSet                                                                           // Getters and setters for the root
    {
        public string @event { get; set; }
        public string url { get; set; }
        public long seed { get; set; }
        public GetSet getSet { get; set; }
    }

    //Resources and Tutorials used
    /* https://docs.unity3d.com/Manual/editor-CustomEditors.html
     * https://learn.unity.com/tutorial/editor-scripting#5c7f8528edbc2a002053b644
     * https://docs.unity3d.com/Manual/UnityWebRequest.html
     * https://www.youtube.com/watch?v=uFjiNkYhBvY
     * https://www.newtonsoft.com/json/help/html/SerializationGuide.htm
     * https://stackoverflow.com/questions/46003824/sending-http-requests-in-c-sharp-with-unity
     * https://answers.unity.com/questions/570100/using-streamreader.html
     * https://docs.unity3d.com/ScriptReference/ImageConversion.html
     * https://blog.cyberiansoftware.com.ar/post/149707644965/web-requests-from-unity-editor
     * https://json2csharp.com/
     * https://answers.unity.com/questions/988174/create-modify-texture2d-to-readwrite-enabled-at-ru.html?childToView=1708382#answer-1708382
     * https://anduin.aiursoft.cn/post/2020/10/13/how-to-serialize-json-object-in-c-without-newtonsoft-json
     */
}
