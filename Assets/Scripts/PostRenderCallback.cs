using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostRenderCallback : MonoBehaviour
{
    public Automata2D callTo;

    private void OnPostRender() {
        callTo.PostRenderCall();
    }
}
