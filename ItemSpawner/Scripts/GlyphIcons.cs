using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSpawner
{
    /// <summary>
    /// Pixel "-" and "+" icons for the level buttons, drawn at runtime over each button state's original
    /// icon: same canvas, pivot, pixels-per-unit, centre and colours (main colour plus the darker shade
    /// row under the strokes, like the vanilla arrow).
    /// </summary>
    public static class GlyphIcons
    {
        private static readonly Color32 FallbackMain=new Color32(102,64,27,255), FallbackShade=new Color32(76,47,18,255);
        private static readonly Color32 Tick=new Color32(255,211,106,255);
        private const int Size=8, Stroke=2;

        public enum Shape { Minus, Plus, Equals, Box, CheckedBox }

        public static void Apply(GameObject button,bool plus) => Apply(button,plus ? Shape.Plus : Shape.Minus);

        public static void Apply(GameObject button,Shape shape)
        {
            foreach(var renderer in button.GetComponentsInChildren<SpriteRenderer>(true)) {
                if(renderer.name!="Icon" || renderer.sprite==null) continue;
                try { renderer.sprite=Draw(renderer.sprite,shape); }
                catch(Exception e) { Debug.LogWarning($"[{ItemSpawnerMod.Name}] Could not draw icon: {e.Message}"); }
            }
        }

        /// <summary>A glyph drawn over <paramref name="original"/>'s canvas (for icons that switch, e.g. a checkbox).</summary>
        /// <param name="colour">Overrides the sampled colour (the shade is derived from it).</param>
        public static Sprite Make(Sprite original,Shape shape,Color32? colour=null)
        {
            try { return Draw(original,shape,colour); }
            catch(Exception e) { Debug.LogWarning($"[{ItemSpawnerMod.Name}] Could not draw icon: {e.Message}"); return original; }
        }

        private static Sprite Draw(Sprite original,Shape shape,Color32? colour=null)
        {
            int w=(int)original.rect.width, h=(int)original.rect.height;
            var pixels=ReadPixels(original,w,h);
            Color32 main=FallbackMain, shade=FallbackShade;
            int cx=w/2, cy=h/2;
            if(pixels!=null) {
                var opaque=Enumerable.Range(0,pixels.Length).Where(i=>pixels[i].a>128).ToList();
                if(opaque.Count>0) {
                    var counts=opaque.GroupBy(i=>Key(pixels[i])).OrderByDescending(g=>g.Count()).ToList();
                    main=pixels[counts[0].First()];
                    var darker=counts.Skip(1).Select(g=>pixels[g.First()]).FirstOrDefault(c=>Bright(c)<Bright(main));
                    shade=darker.a>0 ? darker : new Color32((byte)(main.r*.75f),(byte)(main.g*.75f),(byte)(main.b*.75f),main.a);
                    // Centre on the original glyph, not the canvas.
                    int minX=opaque.Min(i=>i%w), maxX=opaque.Max(i=>i%w), minY=opaque.Min(i=>i/w), maxY=opaque.Max(i=>i/w);
                    cx=(minX+maxX+1)/2; cy=(minY+maxY+1)/2;
                }
            }
            if(colour.HasValue) {
                main=colour.Value;
                shade=new Color32((byte)(main.r*.6f),(byte)(main.g*.6f),(byte)(main.b*.6f),main.a);
            }
            var glyph=new bool[w*h]; var tick=new bool[w*h];
            void Fill(bool[] into,int x0,int y0,int x1,int y1) {
                for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++) if(x>=0 && y>=0 && x<w && y<h) into[y*w+x]=true;
            }
            int l=cx-Size/2, b=cy-Size/2, r=cx+Size/2, t=cy+Size/2;
            switch(shape) {
                case Shape.Minus: case Shape.Plus:
                    Fill(glyph,l,cy-Stroke/2,r,cy+Stroke/2);
                    if(shape==Shape.Plus) Fill(glyph,cx-Stroke/2,b,cx+Stroke/2,t);
                    break;
                case Shape.Equals:
                    Fill(glyph,l,cy+1,r,cy+1+Stroke); Fill(glyph,l,cy-1-Stroke,r,cy-1);
                    break;
                default:
                    // 1-pixel square outline; the checked state adds a gold tick inside (rows run bottom-up).
                    Fill(glyph,l,b,r,b+1); Fill(glyph,l,t-1,r,t); Fill(glyph,l,b,l+1,t); Fill(glyph,r-1,b,r,t);
                    if(shape==Shape.CheckedBox)
                        foreach(var (x,y) in new[]{(1,4),(2,3),(3,2),(4,3),(5,4),(6,5)}) Fill(tick,l+x,b+y-1,l+x+1,b+y+1);
                    break;
            }
            var output=new Color32[w*h];
            for(int i=0;i<output.Length;i++) {
                if(tick[i]) output[i]=Tick;
                else if(glyph[i]) output[i]=main;
                else if(i+w<output.Length && glyph[i+w]) output[i]=shade; // texture rows run bottom-up: shade below
                else output[i]=new Color32(0,0,0,0);
            }
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
            tex.SetPixels32(output); tex.Apply();
            var pivot=new Vector2(original.pivot.x/w,original.pivot.y/h);
            return Sprite.Create(tex,new Rect(0,0,w,h),pivot,original.pixelsPerUnit);
        }

        // Bundle textures aren't CPU-readable; copy through a render texture to sample the original colours.
        private static Color32[] ReadPixels(Sprite sprite,int w,int h)
        {
            RenderTexture rt=null; var previous=RenderTexture.active;
            try {
                var source=sprite.texture;
                rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Graphics.Blit(source,rt);
                RenderTexture.active=rt;
                var copy=new Texture2D(w,h,TextureFormat.RGBA32,false);
                copy.ReadPixels(new Rect(sprite.textureRect.x,sprite.textureRect.y,w,h),0,0);
                copy.Apply();
                var result=copy.GetPixels32();
                UnityEngine.Object.Destroy(copy);
                return result;
            }
            catch(Exception) { return null; }
            finally {
                RenderTexture.active=previous;
                if(rt!=null) RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static int Key(Color32 c)=>(c.r<<16)|(c.g<<8)|c.b;
        private static int Bright(Color32 c)=>c.r+c.g+c.b;
    }
}
