using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

public class PromptBuilderWindow : EditorWindow
{
    public static bool isFromNegativePrompt = false;

    private string activeCategory = "";
    private Vector2 scrollPosition;
    private string[] currentDescriptors = new string[] {};
    private int pageIndex = 0;
    private int itemsPerPage = 25;
    private List<string> tags = new List<string>();
    private string inputText = "";
    private int dragFromIndex = -1;
    private Vector2 dragStartPos;
    private List<Rect> tagRects = new List<Rect>();

    public static Action<string> onReturn;
    public static PromptBuilderWindow Instance;

    [MenuItem("Window/Scenario/Prompt Builder")]
    public static void ShowWindow()
    {
        Instance = GetWindow<PromptBuilderWindow>("Prompt Builder");
    }

    void OnGUI()
    {
        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        tagRects.Clear();

        EditorGUILayout.LabelField("Prompt with tags");
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle customTagStyle = new GUIStyle(EditorStyles.label);
                customTagStyle.fixedHeight = 25;
                customTagStyle.margin = new RectOffset(0, 5, 0, 5);

                float availableWidth = EditorGUIUtility.currentViewWidth - 20;
                int tagsPerRow = Mathf.FloorToInt(availableWidth / 100);
                int currentTagIndex = 0;

                while (currentTagIndex < tags.Count)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < tagsPerRow && currentTagIndex < tags.Count; i++)
                    {
                        Rect tagRect = GUILayoutUtility.GetRect(new GUIContent(tags[currentTagIndex]), customTagStyle);

                        bool isActiveTag = currentTagIndex == dragFromIndex;
                        GUIStyle tagStyle = isActiveTag ? new GUIStyle(customTagStyle) { normal = { background = MakeTex(2, 2, isActiveTag ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.8f, 0.8f, 0.8f)) } } : customTagStyle;

                        Rect xRect = new Rect(tagRect.xMax - 20, tagRect.y, 20, tagRect.height);

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tagRect.Contains(Event.current.mousePosition))
                        {
                            if (!xRect.Contains(Event.current.mousePosition))
                            {
                                dragFromIndex = currentTagIndex;
                                dragStartPos = Event.current.mousePosition;
                                Event.current.Use();
                            }
                        }

                        if (dragFromIndex >= 0 && Event.current.type == EventType.MouseDrag)
                        {
                            int newIndex = GetNewIndex(Event.current.mousePosition);
                            if (newIndex != -1 && newIndex != dragFromIndex)
                            {
                                string tempTag = tags[dragFromIndex];
                                tags.RemoveAt(dragFromIndex);
                                tags.Insert(newIndex, tempTag);
                                dragFromIndex = newIndex;
                            }
                        }

                        if (Event.current.type == EventType.MouseUp)
                        {
                            dragFromIndex = -1;
                        }

                        EditorGUI.LabelField(tagRect, tags[currentTagIndex], tagStyle);

                        if (GUI.Button(xRect, "x"))
                        {
                            tags.RemoveAt(currentTagIndex);
                        }
                        else
                        {
                            currentTagIndex++;
                        }

                        tagRects.Add(tagRect);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            {
                GUI.SetNextControlName("inputTextField");
                inputText = EditorGUILayout.TextField(inputText, GUILayout.ExpandWidth(true), GUILayout.Height(25));

                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    if (GUI.GetNameOfFocusedControl() == "inputTextField")
                    {
                        if (!string.IsNullOrWhiteSpace(inputText))
                        {
                            string descriptorName = inputText.Trim();
                            tags.Add(descriptorName);
                            inputText = "";
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        EditorGUI.FocusTextInControl("inputTextField");
                        Event.current.Use();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20f);

        GUILayout.Label("Enhance your prompts with additional tokens from these categories:");

        DisplayCategoryButtons();

        EditorGUILayout.Space(20f);

        GUILayout.Label("Choose a descriptor to add to your prompt");

        DisplayDescriptorButtons(currentDescriptors.Skip(pageIndex * itemsPerPage).Take(itemsPerPage).ToArray());

        EditorGUILayout.Space(20f);

        DisplayNavigationButtons();

        GUIStyle updateButtonStyle = new GUIStyle(GUI.skin.button);
        updateButtonStyle.normal.background = CreateColorTexture(new Color(0, 0.5333f, 0.8f, 1));
        updateButtonStyle.normal.textColor = Color.white;
        updateButtonStyle.active.background = CreateColorTexture(new Color(0, 0.3f, 0.6f, 1));
        updateButtonStyle.active.textColor = Color.white;

        Rect bottomAreaRect = new Rect(0, Screen.height - 60, Screen.width, 60);

        GUILayout.BeginArea(bottomAreaRect);

        if (onReturn != null)
        {
            if (GUILayout.Button("Update Tags", updateButtonStyle, GUILayout.Height(40)))
            {
                Close();

                onReturn?.Invoke(SerializeTags());
                onReturn = null;
            }
        }

        GUILayout.EndArea();
    }

    public string SerializeTags()
    {
        string str = "";
        foreach (var tag in tags)
        {
            str += tag + ", ";
        }
        return str;
    }

    int GetNewIndex(Vector2 currentPos)
    {
        for (int i = 0; i < tagRects.Count; i++)
        {
            if (tagRects[i].Contains(currentPos))
            {
                return i;
            }
        }

        return -1;
    }

    void DisplayCategoryButtons()
    {
        string[] categories = PromptData.GetCategories();
        float buttonWidth = 100;
        int buttonsPerRow = Mathf.FloorToInt(position.width / buttonWidth);
        int currentButton = 0;
        GUIStyle activeButtonStyle = new GUIStyle(GUI.skin.button);
        activeButtonStyle.normal.textColor = Color.blue;

        for (int i = 0; i < categories.Length; i++)
        {
            if (currentButton % buttonsPerRow == 0)
            {
                EditorGUILayout.BeginHorizontal();
            }

            bool isActiveCategory = categories[i] == activeCategory;
            GUIStyle buttonStyle = isActiveCategory ? activeButtonStyle : GUI.skin.button;
            if (GUILayout.Toggle(isActiveCategory, categories[i], buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (!isActiveCategory)
                {
                    activeCategory = categories[i];
                    currentDescriptors = PromptData.GetDescriptorsForCategory(activeCategory);
                    scrollPosition = Vector2.zero;
                }
            }

            currentButton++;

            if (currentButton % buttonsPerRow == 0 || i == categories.Length - 1)
            {
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void DisplayDescriptorButtons(string[] descriptors)
    {
        if (descriptors.Count() <= 0)
        {
            return;
        }

        int maxCharsPerLine = 20;
        int buttonWidthPerChar = 8;

        float maxButtonWidth = descriptors.Max(d => Mathf.Min(d.Length, maxCharsPerLine) * buttonWidthPerChar);

        int buttonsPerRow = Mathf.FloorToInt(position.width / maxButtonWidth);
        int currentButton = 0;

        EditorGUILayout.BeginVertical();
        for (int i = 0; i < descriptors.Length; i++)
        {
            if (currentButton % buttonsPerRow == 0)
            {
                EditorGUILayout.BeginHorizontal();
            }

            DisplayDescriptorButton(descriptors[i], Mathf.FloorToInt(position.width / buttonsPerRow));

            currentButton++;

            if (currentButton % buttonsPerRow == 0 || i == descriptors.Length - 1)
            {
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
    }


    void DisplayDescriptorButton(string descriptorName, int maxWidth)
    {
        if (GUILayout.Button(descriptorName, GUILayout.Width(maxWidth), GUILayout.Height(25)))
        {
            tags.Add(descriptorName);
        }
    }

    void DisplayNavigationButtons()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(pageIndex == 0);
        if (GUILayout.Button("Previous", GUILayout.Width(100), GUILayout.Height(25)))
        {
            pageIndex--;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(pageIndex >= Mathf.CeilToInt((float)currentDescriptors.Length / itemsPerPage) - 1);
        if (GUILayout.Button("Next", GUILayout.Width(100), GUILayout.Height(25)))
        {
            pageIndex++;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = color;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    internal static void ShowWindow(Action<string> onPromptBuilderReturn, List<string> tagList)
    {
        ShowWindow();
        Instance.tags = tagList;
        onReturn += onPromptBuilderReturn;
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}

public class ResponsiveGridItem
{
    public string name;
    public GUIStyle style;

    public ResponsiveGridItem(string name, GUIStyle style)
    {
        this.name = name;
        this.style = style;
    }
}


public static class PromptData
{
    public static string[] GetCategories()
    {
        return new string[] { "Art Style", "Art Medium", "Describers", "Colors", "Materials", "Lighting", "Framing", "Render", "Details", "Epoch", "Weather", "Aesthetics", "Others" };
    }

    public static string[] GetDescriptorsForCategory(string category)
    {
        switch (category)
        {
            case "Art Style":
                return new string[] { "abstract", "anime", "art deco", "bauhaus", "brutalism", "comic book style", "contemporary",
                "cubism", "cyberpunk", "fantasy", "fine lining", "futurism", "impressionism", "isometric",
                "kawaii", "low-poly", "manga", "pixel art", "pop art", "retro", "sci-fi", "sharp lines",
                "steampunk", "surrealism", "vector", "voxel art" };
            case "Art Medium":
                return new string[] { "acrylic paint", "amigurumi", "cartoon", "chalk art", "character design", "concept art",
                "concept design", "digital art", "diorama", "drawing", "fine art", "gouache", "graffiti",
                "illustration", "Japanese print", "oil", "painting", "paper model", "paper-marbling",
                "papercutting", "sculpture", "sketch", "street art", "watercolor" };
            case "Describers":
                return new string[] { "apocalyptic", "beautiful", "big", "centered", "chaotic", "delicate", "distorted",
                "geometric", "gloomy", "intricate", "kaleidoscopic", "magical", "minimalist",
                "monumental", "organic", "ornate", "psychedelic", "sad", "scary", "small", "somber",
                "symmetric", "tall", "trending on Artstation", "trending on Pixiv" };
            case "Colors":
                return new string[] { "aquamarine", "azure", "black", "blue", "brown", "charcoal", "color sharding",
                "colorful", "contrasted", "coral", "cyan", "desaturated", "fuchsia", "gray",
                "green", "hot pink magenta", "hue colors", "ivory", "khaki", "lime", "maroon",
                "multicolor", "navy blue", "olive", "orange", "plum", "purple", "rainbow",
                "red", "saturated", "silver", "teal", "vivid", "white", "yellow" };
            case "Materials":
                return new string[] { "aluminum", "bronze", "clay", "concrete", "copper", "dirt", "epoxy resin",
                "fabric", "foam", "glass", "gold", "iron", "lead", "marble", "metal",
                "nickel", "paper", "plastic", "plasticine", "rubber", "sand", "silver",
                "stainless steel", "stone", "titanium", "wood", "zinc" };
            case "Lighting":
                return new string[] { "Christmas light", "Xray", "backlight", "blacklight", "blue lighting", "bright", "candlelight",
                "cinematic lighting", "colorful lighting", "concert lighting", "contre-jour", "crepuscular rays",
                "dark", "dark lighting", "dawn", "daylight", "dim lighting", "direct sunlight", "dramatic lighting",
                "fairy lights", "glowing", "glowing radioactively", "golden hour", "incandescent light", "moonlight",
                "neon", "nightclub lighting", "nuclear waste glow", "quantum dot display", "realistic lighting",
                "soft lighting", "spotlight", "strobe", "studio lighting", "sunlight", "sunrise", "sunset", "twilight",
                "ultraviolet", "ultraviolet light", "volumetric lighting" };
            case "Framing":
                return new string[] { "GoPro view", "aerial view", "body shot", "close-up", "close-up portrait", "cow-boy shot", "face shot", "fisheye",
                "full body shot", "landscape", "long shot", "low angle", "lower body shot", "massive scale", "panoramic", "portrait",
                "side view", "street level view", "top-down", "ultra wide-angle", "upper body shot", "wide shot", "wide-angle" };
            case "Render":
                return new string[] { "2d platformer", "3D rendering", "Blender rendering", "CGsociety", "Cinema4D rendering",
                "Houdini rendering", "Octane rendering", "Photoshop", "Redshift rendering", "Sketchfab",
                "Substance 3D", "Unreal Engine rendering", "Zbrush rendering", "cgi", "highly detailed" };
            case "Details":
                return new string[] { "100mm", "16-bit", "4k", "500px", "64-bit", "8-bit", "8k", "DSLR", "HDR",
                "analog photography", "bokeh", "depth of field", "detailed", "film photography",
                "hi-fi", "high quality", "highly detailed", "hyper-realistic", "lo-fi", "polaroid",
                "studio quality", "uhd", "ultra realistic" };
            case "Epoch":
                return new string[] { "1100s", "Assyrian Empire", "Aztec", "Babylonian Empire", "Benin Kingdom", "Bronze Age", 
                "Byzantine Empire", "Carolingian", "Dark Ages", "Edwardian", "Elizabethan", "Georgian", "Gilded Age", 
                "Great Depression", "Heian Period", "Incan", "Industrial Revolution", "Iron Age", "Maori", "Mayan", 
                "Meiji Period", "Middle Ages", "Ming Dynasty", "Minoan", "Moorish", "Mughal Era", "Nasrid", "Navajo", 
                "Neolithic", "Olmec", "Ottoman Empire", "Paleolithic", "Persian Empire", "Primitive society", 
                "Qing Dynasty", "Regency", "Renaissance", "Shang Dynasty", "Songhai", "Stone Age", "Sumerian", 
                "Tokugawa Shogunate", "Tudor", "Victorian", "Viking", "World War I", "World War II", "Zhou Dynasty", 
                "Zuni-Pueblo", "ancient Egypt", "ancient Greece", "ancient Rome", "antique", "antiquity", "aztec", 
                "bronze age", "contemporary", "future", "medieval", "mid-century", "middle ages", "modern", 
                "modern world", "pre-Columbian", "prehistoric", "renaissance", "retro", "sci-fi", "space age", "victorian", "viking" };
            case "Weather":
                return new string[] { "cloudy", "foggy", "frosty", "humid", "icy", "rainy", "snowy", "stormy", "sunny", "windy" };
            case "Aesthetics":
                return new string[] { "Abstract Tech", "Acid Pixie", "Acidwave", "Acubi", "Adventure Pulp", 
                "Adventurecore", "Aetherpunk", "Afro-Victorian", "Afrofuturism", "After Hours", "Agropeople", "Alien", 
                "Alternative", "American Pioneers", "American Revolution", "American Thanksgiving", 
                "American Tourist Traps", "Americana", "Analog Horror", "Ancient Egypt", "Androgynous", "Anemoiacore", 
                "Angelcore", "Anglo Gothic", "Angura Kei", "Animecore", "Anti-Fashion", "Antique Grunge", "Arcade", 
                "Arcadecore", "Art Academia", "Art Deco", "Art Hoe", "Art Nouveau", "Arts and Crafts Movement", 
                "Athlete", "Atompunk", "Auroracore", "Autumn", "Autumn Academia", "Avant Apocalypse", "Avant-garde", 
                "Back-to-School", "Baddie", "Ballet Academia", "Baltic Violence Tumblr", "Barbiecore", "Bardcore", 
                "Baroque", "Bastardcore", "Bauhaus", "Beach Day", "Beatnik", "Biker", "Bimbocore", "Biohazard", "Biopunk", 
                "Bizarro Fiction", "Black-Holed Meme", "Bodikon", "Bohemian", "Bronzepunk", "C-Pop", "Camp", 
                "Campcore", "Candycore", "Cargopunk", "Carnivalcore", "Cartoon", "Cartooncore", "Casino", 
                "Cassette Futurism", "Celtic", "Changelingcore", "Chaotic Academia", "Chav", "Cheiron Crush", 
                "Cholo", "Christmas", "Chunyu", "City Pop", "Classic Academia", "Classic Lolita", "Classicism", 
                "Cleancore", "Clockpunk", "Cloudcore", "Clowncore", "Club", "Club Kids", "Coastal Grandmother", 
                "Coffee House/Cafe", "Coffinwood", "Colourful Black", "Comfy/Cozy", "Concore", "Constructivism", 
                "Coquette", "Coquette Academia", "Corporate", "Corporate Memphis", "Corporate Punk", "Cottagecore", 
                "Cottagegore", "Country", "Cozy Childhood Hideaway", "Craftcore", "Cripplepunk", "Crowcore", 
                "Crustpunk", "Cryptid Academia", "Cryptidcore", "Cubism", "Cuddle Party", "Cult Party Kei", 
                "Cultcore", "Cutecore", "Cyber Fairy Grunge", "Cyberdelic", "Cyberghetto", "Cybergoth", 
                "Cybergrunge", "CyberneticPunk", "Cyberparadism", "Cyberpop", "Cyberprep", "Cyberpunk", 
                "Danish Pastel", "Dark Academia", "Dark Fantasy", "Dark Naturalism", "Dark Nautical", 
                "Dark Nymphet", "Dark Paradise", "DarkErrorcore", "Darkcore", "Darkest Academia", 
                "Daydreampunk", "Dazecore", "De Stijl", "Deathcore", "Deathrock", "Decopunk", "Decora", 
                "Delicate Sweet", "Desertwave", "Desi Romantic Academia", "Dethereal", "Devilcore", "Dieselpunk", 
                "Diner", "Dionysism", "Dolly Kei", "Dracopunk", "Dragoncore", "Drain", "Dreamcore", "Dreamy", 
                "Drugcore", "Dual Kawaii", "Duckcore", "Dullcore", "Dungeon Synth", "Earthcore", "Electro Swing", 
                "ElectroPop 08", "Emo", "English Major", "Equestrian", "Erokawa", "Ethereal", "Europunk", 
                "Expressionism", "Fairy Academia", "Fairy Grunge", "Fairy Kei", "Fairy Tale", "Fairycore", "Fanfare", 
                "Fantasy", "Fantasy Astronomy", "Farmer's Daughter", "Fashwave", "Fauvism", "Fawncore", "Femme Fatale", 
                "Feralcore", "Film Noir", "Flapper", "Flat Design", "Folk Punk", "Foodie", "Forestpunk", "French", 
                "Frogcore", "Frutiger Aero", "Funky Seasons", "Furry", "Futago", "Futurism", "Gadgetpunk", "Game Night", 
                "Gamercore", "Gamine", "Geek", "Gen X Soft Club", "Ghostcore", "Glam Rock", "Glitchcore", "Gloomcore", 
                "Glowwave", "Goblin Academia", "Goblincore", "Golden Age of Detective Fiction", "Golden Hour", "Gopnik", 
                "Gorecore", "Gorpcore", "Goth", "Gothcore", "Gothic", "Gothic Lolita", "Grandmillenial", "Grandparentcore", 
                "Greaser", "Green Academia", "Grifes", "Grindhouse", "Groundcore", "Grunge", "Gurokawa", "Gyaru", 
                "Hackercore", "Halloween", "Hallyu", "Happycore", "Hatecore", "Hauntology", "Haussmann Paris", "Health Goth", 
                "Heatwave", "Heistcore", "Hellenic", "Hermaphroditus", "Hermitpunk", "Hexatron", "Hi-NRG", "High School Dream", 
                "Hikecore", "Hime Lolita", "Hip-Hop", "Hipness Purgatory", "Hippie", "Hipster", "Hispanicore", "Historical Americana", 
                "Holosexual", "Honeycore", "Horror", "Horror Academia", "Hot Topic", "Hustlewave", "Hydrogen", "Hyperpop", "Icepunk", 
                "Imaginarium", "Impressionism", "Indicolite", "Indie", "Indie Kid", "Indie Sleaze", "Internet Academia", 
                "Italian Mafia", "Italian Renaissance", "Italo Disco", "Jamcore", "Japanese High School", "Jersey Shore", "Joyride", 
                "Juggalo", "Jungle Grunge", "Junglecore", "Karasu Zoku", "Kawaii", "Kawaii Gamer", "Key West Kitten", "Kid Science", 
                "Kidcore", "Kimoicore", "Kinderwhore", "King Gas", "Kingcore", "Knightcore", "Kogal", "Kuromicore", "La Sape", 
                "Labcore", "Laborwave", "Lagenlook", "Larme Kei", "Larme Kei", "Late 2000s Elementary School", "Libertywave", 
                "Light Academia", "Lightcore", "Lightningwave", "Liminal Space", "Lit Kid", "Lo-Fi", "Long Island", "Lounge", 
                "Lovecore", "Lunarpunk", "MTV Green Sky", "Macaute", "Mad Scientist", "Magewave", "Magical", "Maidcore", "Mall Ninja", 
                "Mallgoth", "Maximalism", "McBling", "Meatcore", "Medicalcore", "Medieval", "Memphis", "Mermaid", "Metal", "Metalcore", 
                "Metalheart", "Metrosexual", "Miami Metro", "Midwest Emo", "Midwest Gothic", "Military", "Milk", "Miniaturecore", "Minimalism", 
                "Minivan Rock", "Miscellaneous Academia", "Mizuiro Kaiwai", "Mod", "Modern Brazil", "Modernism", "Mori Kei", "Morute", 
                "Mosscore", "Mote Kei", "Ms Paint", "Mulchcore", "Mushroomcore", "Musical Academia", "Mythpunk", "Nanopunk", "Naturecore",
                "Nautical", "Neko", "Neo-Romanism", "Neo-Tokyo", "Nerd", "Nerdcore", "New Age", "New England Gothic", "New Money", "New Romantic",
                "New Wave", "Nihilcore", "Nintencore", "Normcore", "Northerness", "Nostalgiacore", "Nu-Goth", "Nuclear", "Nymphet", "Ocean Academia",
                "Ocean Grunge", "Old Hollywood", "Old Memecore", "Old Money", "Old Web", "Onii Kei", "Oshare Kei", "Otaku", "Otherkin", "PC Music",
                "Pachuco", "Pale Grunge", "Paleocore", "Palewave", "Paramilitary", "Party Animal", "Party Kei", "Pastel", "Pastel Academia", 
                "Pastel Goth", "Pastel Punk", "Peach", "Pearly", "Peoplehood", "Photorealism", "Pin-up", "Pink Parisian", "Pink Pilates Princess", 
                "Pink Princess", "Pinterest Coquette", "Pirate", "Pixel Cutie", "Pixiecore", "Plaguecore", "Plant Mom", "Pop", "Pop Art", "Pop Kei",
                "Post-Apocalyptic", "Post-Impressionism", "Post-Punk", "Post-rock", "Powwow Step", "Prairiecore", "Pre-Raphaelite", "Prehistoricore",
                "Pride flags", "Princecore", "Princesscore", "Printcore", "Progressive Academia", "Psychedelica", "Punk", "Purism", "Quality Tumblr",
                "Queencore", "Queer Academia", "Queercore", "R&B", "Racaille", "Ragecore", "Rainbowcore", "Rainy Day", "Randumb","Rangercore",
                "Ratcore", "Ravencore", "Raver", "Real Life Super Hero", "Realism", "Reefwave", "Regency", "Regional Gothic", "Retro-Futurism", "Rivethead", 
                "Roaring 20s", "Robotics Kid", "Rock", "Rockabilly", "Rococo", "Roguecore", "Rollerwave", "Roma", "Romantic Academia", "Romantic Goth", 
                "Romantic Italian", "Romanticism", "Rotcore", "Royalcore", "Rusticcore", "Sad people", "Salon Kei", "Salvagepunk", "Sandalpunk",
                "Sanriocore", "Scene", "Schizowave", "Science Academia", "Scoutcore", "Scrapbook", "Scrapper", "Seapunk","Selkiecore",
                "Shanzhai", "Sharpies", "Shibuya Punk", "Shironuri", "Shoegaze", "Shā mǎ tè", "Shamate", "Sigilkore", "Sizz", "Skater",
                "Sleepycore", "Slimepunk", "Sloanies", "Snow Bunny", "Snowdrop", "Soft Apocalypse", "Soft Grunge", "Soft Macabre", "Soft indie",
                "Softie", "Soggy", "Solarpunk", "Southern Belle", "Southern Gothic", "Space Cowboy", "Spacecore", "Sparklecore", "Spiritcore", 
                "Tacticool", "Takenokozoku", "Tanbi Kei", "Technical Scene", "Technocore", "Technozen", "Techwear", "Teddies", "Teenqueen", 
                "Teethcore", "Terrorwave", "Teslapunk", "Theatre Academia", "Theatre Kids", "Thrasher", "Thriftcore", "Tiki", "Tinkercore", 
                "Tinycore", "Trad Goth", "Traditional Korean", "Trailer Park Princess", "Trenchcore", "Trendercore", "Trillwave", "Tropical", 
                "Tumblewave", "Tupinipunk", "Twee", "Tweencore", "Ukiyo-e", "Unicorncore", "Urban Fantasy", "Urbancore", "Utopian Scholastic", 
                "VSCO", "Vampire", "Vaporwave", "Vectorbloom", "Vectorheart", "Vibrant Academia", "Victorian", "Victorian Goth", "Viking", "Villagecore",
                "Villaincore", "Vintage British Sportsman", "Vintage Parisian", "Virgo's Tears", "Visual Kei", "Voidcore", "Voidpunk", "Vorticism",
                "Vulture Culture", "Wabi-Sabi", "Waif", "Waldorf", "Wanderlust", "Warmcore", "Weathercore", "Web Creep", "Weeaboo", "Weirdcore",
                "Werewolf", "Western", "Wetcore", "Wild Child", "Winter", "Winter Fairy Coquette", "Witch House", "Witchcore", "Witchy Academia",
                "Wizardcore", "Wonderland", "Woodland Goth", "Wormcore", "Writer Academia", "Wuxia", "XO", "Y2K", "Yakuza", "Yami Kawaii",
                "Yandere", "Yankeecore", "Yanki", "Youthquake", "Yume Kawaii", "Zombie Apocalypse" };
            case "Others":
                return new string[] { " " };
            
            default:
                return new string[] { " "};
        }
    }
}