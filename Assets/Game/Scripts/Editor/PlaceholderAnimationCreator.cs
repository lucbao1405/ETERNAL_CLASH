using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class PlaceholderAnimationCreator
{
    [MenuItem("Tools/Create Placeholder Animations")]
    public static void CreateAll()
    {
        CreatePlayerAnimations();
        CreateArcherAnimations();
        CreateSlimeAnimations();
        CreateWolfAnimations();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All placeholder animations created!");
    }
    
    static void CreatePlayerAnimations()
    {
        string basePath = "Assets/Game/Animations/Player";
        string controllerPath = "Assets/Game/Animations/Controllers/Player.controller";
        
        var idleSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/TrangThai/chay.png");
        var runSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/TrangThai/chay.png");
        var attackSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/TrangThai/danh thuong.png");
        var hitSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/TrangThai/bi danh.png");
        var deadSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/TrangThai/Chet.png");
        var skillSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Player/Skill/Charge/charge 1.png");
        
        CreateAnimationClip(basePath + "/Player_Idle.anim", idleSprites, 6);
        CreateAnimationClip(basePath + "/Player_Run.anim", runSprites, 8);
        CreateAnimationClip(basePath + "/Player_Attack.anim", attackSprites, 10);
        CreateAnimationClip(basePath + "/Player_Hit.anim", hitSprites, 8);
        CreateAnimationClip(basePath + "/Player_Dead.anim", deadSprites, 8);
        CreateAnimationClip(basePath + "/Player_Skill.anim", skillSprites, 12);
        
        CreateController(controllerPath, new string[] {
            "Player_Idle", "Player_Run", "Player_Attack", "Player_Skill", "Player_Hit", "Player_Dead"
        });
    }
    
    static void CreateArcherAnimations()
    {
        string basePath = "Assets/Game/Animations/Enemy/Archer";
        string controllerPath = "Assets/Game/Animations/Controllers/Archer.controller";
        
        var idleSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Archer/dung.png");
        var attackSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Archer/ban.png");
        var hitSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Archer/nhan sat thuong.png");
        var deadSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Archer/chet.png");
        var prepSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Archer/chuan bi ban.png");
        
        CreateAnimationClip(basePath + "/Archer_Idle.anim", idleSprites, 6);
        CreateAnimationClip(basePath + "/Archer_Run.anim", idleSprites, 8);
        CreateAnimationClip(basePath + "/Archer_Attack.anim", attackSprites, 10);
        CreateAnimationClip(basePath + "/Archer_Hit.anim", hitSprites, 8);
        CreateAnimationClip(basePath + "/Archer_Dead.anim", deadSprites, 8);
        CreateAnimationClip(basePath + "/Archer_Prepare.anim", prepSprites, 8);
        
        CreateController(controllerPath, new string[] {
            "Archer_Idle", "Archer_Run", "Archer_Attack", "Archer_Prepare", "Archer_Hit", "Archer_Dead"
        });
    }
    
    static void CreateSlimeAnimations()
    {
        string basePath = "Assets/Game/Animations/Enemy/Slime";
        string controllerPath = "Assets/Game/Animations/Controllers/Slime.controller";
        
        var idleSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Slime/dung.png");
        var attackSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Slime/can.png");
        var hitSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Slime/nhan don.png");
        var deadSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Slime/chet.png");
        var prepSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Slime/chuan bi can.png");
        
        CreateAnimationClip(basePath + "/Slime_Idle.anim", idleSprites, 6);
        CreateAnimationClip(basePath + "/Slime_Run.anim", idleSprites, 8);
        CreateAnimationClip(basePath + "/Slime_Attack.anim", attackSprites, 10);
        CreateAnimationClip(basePath + "/Slime_Hit.anim", hitSprites, 8);
        CreateAnimationClip(basePath + "/Slime_Dead.anim", deadSprites, 8);
        CreateAnimationClip(basePath + "/Slime_Prepare.anim", prepSprites, 8);
        
        CreateController(controllerPath, new string[] {
            "Slime_Idle", "Slime_Run", "Slime_Attack", "Slime_Prepare", "Slime_Hit", "Slime_Dead"
        });
    }
    
    static void CreateWolfAnimations()
    {
        string basePath = "Assets/Game/Animations/Enemy/Wolf";
        string controllerPath = "Assets/Game/Animations/Controllers/Wolf.controller";
        
        var idleSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/chay.png");
        var runSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/chay.png");
        var attackSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/can.png");
        var hitSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/nhan don.png");
        var deadSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/chet.png");
        var prepSprites = LoadSprites("Assets/Game/Prefabs/Asset tam thoi/Enemy/Wolf/chuan bi can.png");
        
        CreateAnimationClip(basePath + "/Wolf_Idle.anim", idleSprites, 6);
        CreateAnimationClip(basePath + "/Wolf_Run.anim", runSprites, 8);
        CreateAnimationClip(basePath + "/Wolf_Attack.anim", attackSprites, 10);
        CreateAnimationClip(basePath + "/Wolf_Hit.anim", hitSprites, 8);
        CreateAnimationClip(basePath + "/Wolf_Dead.anim", deadSprites, 8);
        CreateAnimationClip(basePath + "/Wolf_Prepare.anim", prepSprites, 8);
        
        CreateController(controllerPath, new string[] {
            "Wolf_Idle", "Wolf_Run", "Wolf_Attack", "Wolf_Prepare", "Wolf_Hit", "Wolf_Dead"
        });
    }
    
    static Sprite[] LoadSprites(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite s)
                sprites.Add(s);
        }
        return sprites.ToArray();
    }
    
    static void CreateAnimationClip(string path, Sprite[] sprites, float frameRate)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("No sprites for: " + path);
            return;
        }
        
        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;
        
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";
        
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / frameRate;
            keyframes[i].value = sprites[i];
        }
        
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        clip.wrapMode = WrapMode.Loop;
        
        AssetDatabase.CreateAsset(clip, path);
    }
    
    static void CreateController(string path, string[] clipNames)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        AnimatorControllerParameter param = new AnimatorControllerParameter();
        param.name = "State";
        param.type = AnimatorControllerParameterType.Int;
        controller.AddParameter(param);
        
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        AnimatorState[] states = new AnimatorState[clipNames.Length];
        for (int i = 0; i < clipNames.Length; i++)
        {
            states[i] = rootStateMachine.AddState(clipNames[i]);
            string clipPath = path.Replace("Controllers/", "").Replace(".controller", "/" + clipNames[i] + ".anim");
            states[i].motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (i == 0)
                rootStateMachine.defaultState = states[i];
        }
        
        int[] stateIds = new int[] { 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < states.Length; j++)
            {
                if (i == j) continue;
                
                AnimatorStateTransition transition = states[i].AddTransition(states[j]);
                transition.AddCondition(AnimatorConditionMode.Equals, stateIds[j], "State");
                transition.hasExitTime = false;
                transition.duration = 0.1f;
            }
        }
    }
}
