using UnityEngine;

public class SkinController : MonoBehaviour
{
    [System.Serializable]
    public class SkinMapping
    {
        public int dbID;               
        public GameObject skinModel;   
    }

    public SkinMapping[] allSkins;

    private void Start()
    {
        int equippedSkin = PlayerPrefs.GetInt("EquippedSkinID", 1);
        ApplySkin(equippedSkin);
    }

    public void ApplySkin(int skinId)
    {
        bool skinFound = false;
        foreach (var skin in allSkins)
        {
            if (skin.skinModel == null) continue;

            if (skin.dbID == skinId)
            {
                skin.skinModel.SetActive(true);
                skinFound = true;
            }
            else
            {
                skin.skinModel.SetActive(false);
            }
        }

        if (!skinFound && allSkins.Length > 0) ApplySkin(1); 
    }
}