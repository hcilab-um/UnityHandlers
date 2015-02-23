using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{

  public class MenuLauncher : MonoBehaviour
  {

    public GUITexture[] options;

    void Update()
    {
      if (!Input.GetMouseButtonUp(0))
        return;

      Vector2 position = Input.mousePosition;
      if (position.x > (Screen.width / 2))
        position.x -= (Screen.width / 2);

      foreach (GUITexture option in options)
      {
        if (option.HitTest(position))
          Application.LoadLevel(option.name);
      }
    }
  }

}