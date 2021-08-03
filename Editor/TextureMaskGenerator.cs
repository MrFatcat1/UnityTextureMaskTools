using UnityEngine;
using UnityEditor;
using System.IO;

namespace EditorTools{
public class TextureMaskGenerator : EditorWindow
{
    string newMapName = "";
    bool destroyOriginal;
    public Texture2D roughnessMap;
    public Texture2D metalMap;
    public Texture2D aoMap;
    public Texture2D maskMap;

    public Texture2D detailSaturatedAlbedo;
    public Texture2D detailNormalMap;

    public Texture2D bodyMask;
    public Texture2D shirtMask;
    public Texture2D pantsMask;
    public Texture2D shoesMask;

    public Color specularColor = new Color(0.2f, 0.2f, 0.2f);
    public int tab;
    public bool roughnessMethod;

    float specularValue = 0.3f;
    float metallicValue = 0f;

    string extractionMapsPath;
    bool extractAO;
    bool extractSpecular = true;
    bool extractMetallic;
    bool extractDetailMask;


    [MenuItem("Tools/Texture Map Utilities")]
    static void OpenWindow()
    {
        TextureMaskGenerator window = EditorWindow.GetWindow<TextureMaskGenerator>();
        window.maxSize = new Vector2(550,500);
        window.extractionMapsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        window.Show(true);
    }

    private void OnGUI()
    {
        tab = GUILayout.Toolbar(tab,new string[] { "Specular", "Metallic","Hdrp Mask","Mask Extractor","Character Mask","Detail Mask"});
        GUILayout.Space(5);
        EditorGUI.BeginChangeCheck();

        if (tab != 3 && tab != 4)
        {
            roughnessMap = EditorGUILayout.ObjectField("Roughtness Map", roughnessMap, typeof(Texture2D), true) as Texture2D;
            if (EditorGUI.EndChangeCheck() && roughnessMap != null)
            {
                newMapName = (roughnessMap.name.Contains("_") ? roughnessMap.name.Substring(0, roughnessMap.name.LastIndexOf('_')) : roughnessMap.name) + (tab == 0 ? "_SpecularSmoothness" : (tab == 2 ? "_Mask" : "_MetallicSpecular"));
            }
        }

        if (tab == 1) DrawMetallicTab();
        else if (tab == 2) DrawMaskMap();
        else if (tab == 3) DrawExtractionTab();
        else if (tab == 4) DrawCharacterMaskTab();
        else if (tab == 4) DrawCharacterMaskTab();
        else if (tab == 5) DrawDetailMaskTab();

        if (tab != 3 && tab != 4)
        {
            roughnessMethod = EditorGUILayout.Toggle("Use Roughness Method", roughnessMethod);
            newMapName = EditorGUILayout.TextField("Output Name", newMapName);
            destroyOriginal = EditorGUILayout.Toggle("Destroy Original Texture", destroyOriginal);
        }

        if (tab == 0 && GUILayout.Button("Generate Specular Map") && roughnessMap != null)
            GenerateSpecular();
        else if (tab == 1 && GUILayout.Button("Generate Metallic Map") && roughnessMap != null)
            GenerateMetallicMap();
        else if (tab == 2 && GUILayout.Button("Generate Mask Map"))
            GenerateMask();
        else if (tab == 3 && GUILayout.Button("Extract Maps"))
            ExtractMask();
        else if (tab == 4 && GUILayout.Button("Generate Character Map"))
            GenerateCharaterMask();
        else if (tab == 5 && GUILayout.Button("Generate Detail Mask"))
            GenerateDetailMask();
    }

    void DrawMetallicTab()
    {
        metalMap = EditorGUILayout.ObjectField("Metallic Map", metalMap, typeof(Texture2D), true) as Texture2D;
        if (metalMap == null)
            metallicValue = EditorGUILayout.Slider("Metal Value",metallicValue,0,1);
    }

    void DrawMaskMap()
    {
        if (roughnessMap == null)
            specularValue = EditorGUILayout.Slider("Specular Value", specularValue, 0, 1);
        DrawMetallicTab();
        aoMap = EditorGUILayout.ObjectField("Occlussion Map", aoMap, typeof(Texture2D), true) as Texture2D;
       
    }

    void DrawExtractionTab()
    {
        EditorGUI.BeginChangeCheck();
        maskMap = EditorGUILayout.ObjectField("Mask Map",maskMap,typeof(Texture2D),true) as Texture2D;
        if (EditorGUI.EndChangeCheck())
        {
            newMapName = maskMap != null ? maskMap.name.Split('_')[0] : "";
        }
        extractAO = EditorGUILayout.Toggle("Extract AO",extractAO);
        extractMetallic = EditorGUILayout.Toggle("Extract Metallic", extractMetallic);
        extractSpecular = EditorGUILayout.Toggle("Extract Specular", extractSpecular);
        extractDetailMask = EditorGUILayout.Toggle("Extract Detail Mask", extractDetailMask);

        GUILayout.Space(5);
        extractionMapsPath = EditorGUILayout.TextField("Output:", extractionMapsPath);
        newMapName = EditorGUILayout.TextField("Map Name:", newMapName);
    }

    void DrawCharacterMaskTab()
    {
        EditorGUI.BeginChangeCheck();
        bodyMask = EditorGUILayout.ObjectField("Body Mask", bodyMask, typeof(Texture2D),true) as Texture2D;
        if (EditorGUI.EndChangeCheck())
            newMapName = (bodyMask != null ? bodyMask.name.Split('_')[0] : "") + "_CharacterMask";

        shirtMask = EditorGUILayout.ObjectField("Shirt Mask",shirtMask,typeof(Texture2D),true) as Texture2D;
        pantsMask = EditorGUILayout.ObjectField("Pants Mask",pantsMask,typeof(Texture2D),true) as Texture2D;
        shoesMask = EditorGUILayout.ObjectField("Shoes Mask", shoesMask, typeof(Texture2D),true) as Texture2D;

        GUILayout.Space(4);
        newMapName = EditorGUILayout.TextField("Output Name", newMapName);
        EditorGUILayout.HelpBox("R: Body Mask  |  G: Shirt Mask  |  B: PantsMask  |  A:Shoes Mask",MessageType.Info);
        GUILayout.Space(10);
    }

    void DrawDetailMaskTab()
    {
        detailSaturatedAlbedo = EditorGUILayout.ObjectField("Albedo Map", detailSaturatedAlbedo, typeof(Texture2D), true) as Texture2D;
        detailNormalMap = EditorGUILayout.ObjectField("Normal Map", detailNormalMap, typeof(Texture2D), true) as Texture2D;

        GUILayout.Space(4);
        EditorGUILayout.HelpBox("R: Desaturated Albedo  |  G: Normal(G)  |  B: Smoothness  |  A: Normal(R)", MessageType.Info);
        GUILayout.Space(10);
    }

    void GenerateDetailMask()
    {
        if (roughnessMap == null || detailNormalMap == null) return;

        string imagePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(roughnessMap)) + "\\" + newMapName + ".png";
        Texture2D newTexture = new Texture2D(roughnessMap.width, roughnessMap.height);

        Texture2D specularReadable = GetTextureCopy(roughnessMap);
        Texture2D normalReadable = GetTextureCopy(detailNormalMap);
        Texture2D albedoReadable = detailSaturatedAlbedo ? GetTextureCopy(detailSaturatedAlbedo) : null;

        for (int x = 0; x < newTexture.width; x++)
        {
            for (int y = 0; y < newTexture.height; y++)
            {
                Color albedoColor = albedoReadable ? albedoReadable.GetPixel(x, y) : Color.white;
                float desaturatedVal = (albedoColor.r + albedoColor.g + albedoColor.b) / 3;
                
                Color normalColor = normalReadable.GetPixel(x, y);

                float specVal = specularReadable.GetPixel(x, y).r;

                //R - Desaturated Albedo | G - Normal (G) | B - Smoothness  | A - Normal(R)
                Color pixel = new Color(desaturatedVal, normalColor.a, roughnessMethod ? 1 - specVal : specVal,normalColor.b);
                newTexture.SetPixel(x, y, pixel);
            }
        }

        newTexture.Apply();
        byte[] encodedImg = newTexture.EncodeToPNG();

        File.WriteAllBytes(imagePath, encodedImg);
        AssetDatabase.Refresh();
    }

    void GenerateMask()
    {
        Texture2D metalReadable = GetTextureCopy(metalMap);
        Texture2D specularReadable = GetTextureCopy(roughnessMap);
        Texture2D aoReadable = GetTextureCopy(aoMap);
        string imagePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(roughnessMap)) + "\\" + newMapName + ".png";
        Texture2D newTexture = new Texture2D(roughnessMap.width, roughnessMap.height);
        for (int x = 0; x < newTexture.width; x++)
        {
            for (int y = 0; y < newTexture.height; y++)
            {
                float specVal = specularReadable ? specularReadable.GetPixel(x, y).r : specularValue;
                float metalVal = metalReadable ? metalReadable.GetPixel(x, y).r : metallicValue;
                float aoVal = aoReadable ? aoReadable.GetPixel(x, y).r : 1;

                Color pixel = new Color(metalVal, aoVal, 1, roughnessMethod ? 1-specVal:specVal);
                newTexture.SetPixel(x, y, pixel);
            }
        }

        newTexture.Apply();
        byte[] encodedImg = newTexture.EncodeToPNG();

        File.WriteAllBytes(imagePath, encodedImg);

        if (destroyOriginal)
            File.Delete(AssetDatabase.GetAssetPath(roughnessMap));

        AssetDatabase.Refresh();
    }

    void ExtractMask()
    {
        if (!Directory.Exists(extractionMapsPath) || (!extractAO && !extractMetallic && !extractSpecular)) return;

        string ouputSaveFilePath = extractionMapsPath + "\\" + newMapName;
        if (!Directory.Exists(ouputSaveFilePath))
            Directory.CreateDirectory(ouputSaveFilePath);
        
        Texture2D readableTexture = GetTextureCopy(maskMap);

        Texture2D SpecularExtracted = extractSpecular ? new Texture2D(readableTexture.width, readableTexture.height) : null;
        Texture2D OcculssionExtracted = extractAO ? new Texture2D(readableTexture.width, readableTexture.height) : null;
        Texture2D MetallicExtracted = extractMetallic ? new Texture2D(readableTexture.width, readableTexture.height) : null;
        Texture2D DetailExtract = extractDetailMask ? new Texture2D(readableTexture.width, readableTexture.height) : null;

        for (int x = 0; x < readableTexture.width; x++)
        {
            for (int y = 0; y < readableTexture.height; y++)
            {
                Color pixel = readableTexture.GetPixel(x, y);

                if (extractSpecular)
                    SpecularExtracted.SetPixel(x, y, new Color(pixel.a, pixel.a, pixel.a,1));

                if (extractAO)
                    OcculssionExtracted.SetPixel(x, y, new Color(pixel.g, pixel.g, pixel.g, 1));

                if (extractMetallic)
                    MetallicExtracted.SetPixel(x, y, new Color(pixel.r, pixel.r, pixel.r, 1));

                if (extractDetailMask)
                    DetailExtract.SetPixel(x,y,new Color(pixel.b,pixel.b,pixel.b,1));
            }
        }

        if (extractSpecular)
        {
            byte[] encodedImg = SpecularExtracted.EncodeToPNG();
            File.WriteAllBytes(ouputSaveFilePath+"\\"+newMapName+"_Smoothness.png", encodedImg);
        }

        if (extractAO)
        {
            byte[] encodedImg = OcculssionExtracted.EncodeToPNG();
            File.WriteAllBytes(ouputSaveFilePath + "\\" + newMapName + "_Occlussion.png", encodedImg);
        }

        if (extractDetailMask)
        {
            byte[] encodedImg = DetailExtract.EncodeToPNG();
            File.WriteAllBytes(ouputSaveFilePath + "\\" + newMapName + "_DetailMask.png", encodedImg);
        }

        if (extractMetallic)
        {
            byte[] encodedImg = MetallicExtracted.EncodeToPNG();
            File.WriteAllBytes(ouputSaveFilePath + "\\" + newMapName + "_Metallic.png", encodedImg);
        }
    }

    void GenerateCharaterMask()
    {
        if (shirtMask == null || pantsMask == null || shoesMask == null || bodyMask == null) return;
            
        string imagePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(shirtMask)) + "\\" + newMapName + ".png";
        Texture2D newTexture = new Texture2D(shirtMask.width, shirtMask.height);

        Texture2D shirtTex = GetTextureCopy(shirtMask);
        Texture2D pantsTex = GetTextureCopy(pantsMask);
        Texture2D shoesTex = GetTextureCopy(shoesMask);
        Texture2D skinTex = GetTextureCopy(bodyMask);

        for (int x = 0; x < newTexture.width; x++)
        {
            for (int y = 0; y < newTexture.height; y++)
            {
                float bodyVal = skinTex.GetPixel(x, y).r;
                float shirtsVal = shirtTex.GetPixel(x, y).r;
                float pantsVal = pantsTex.GetPixel(x, y).r;
                float shoesVal = shoesTex.GetPixel(x, y).r;

                Color pixel = new Color(bodyVal,shirtsVal,pantsVal,shoesVal);
                newTexture.SetPixel(x, y, pixel);
            }
        }
        newTexture.Apply();
        byte[] encodedImg = newTexture.EncodeToPNG();

        File.WriteAllBytes(imagePath, encodedImg);

        AssetDatabase.Refresh();
    }

    void GenerateSpecular()
    {
        Texture2D readableTexture = GetTextureCopy(roughnessMap);

        //Generate new map
        string imagePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(roughnessMap)) + "\\" + newMapName + ".png";
        Texture2D newTexture = new Texture2D(roughnessMap.width, roughnessMap.height);
        for (int x = 0; x < newTexture.width; x++)
        {
            for (int y = 0; y < newTexture.height; y++)
            {
                float specVal = readableTexture.GetPixel(x, y).r;
                Color pixel = new Color(specularColor.r, specularColor.g, specularColor.b, roughnessMethod ? 1 - specVal : specVal);
                newTexture.SetPixel(x, y, pixel);
            }
        }
        newTexture.Apply();
        byte[] encodedImg = newTexture.EncodeToPNG();

        File.WriteAllBytes(imagePath, encodedImg);

        if (destroyOriginal)
            File.Delete(AssetDatabase.GetAssetPath(roughnessMap));

        AssetDatabase.Refresh();
    }

    void GenerateMetallicMap()
    {
        Texture2D roughTex = GetTextureCopy(roughnessMap);
        Texture2D metalTex = GetTextureCopy(metalMap);

        string imagePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(roughnessMap)) + "\\" + newMapName + ".png";
        Texture2D newTexture = new Texture2D(roughnessMap.width, roughnessMap.height);

        bool useColorAsMetallic = metalMap == null;
        Color constantMetalColor = new Color(metallicValue, metallicValue, metallicValue, 1);
        for (int x = 0; x < newTexture.width; x++)
        {
            for (int y = 0; y < newTexture.height; y++)
            {
                float specVal = roughTex.GetPixel(x, y).r;
                Color pixel = useColorAsMetallic ? constantMetalColor : metalTex.GetPixel(x,y);
                pixel.a =  roughnessMethod ? 1-specVal:specVal;
                newTexture.SetPixel(x, y, pixel);
            }
        }
        newTexture.Apply();
        byte[] encodedImg = newTexture.EncodeToPNG();

        File.WriteAllBytes(imagePath, encodedImg);

        if (destroyOriginal)
            File.Delete(AssetDatabase.GetAssetPath(roughnessMap));

        AssetDatabase.Refresh();
    }

    Texture2D GetTextureCopy(Texture2D source)
    {
        if (source == null) return null;

        byte[] pix = source.GetRawTextureData();
        Texture2D readableText = new Texture2D(source.width, source.height, source.format, false);
        readableText.LoadRawTextureData(pix);
        readableText.name = source.name;
        readableText.Apply();
        return readableText;
    }
}
}