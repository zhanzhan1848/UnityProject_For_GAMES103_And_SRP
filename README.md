# UnityProject_For_GAMES103_And_SRP

This is base shadow map project, and surface wave has not implement(I am soooooooooooooooooooooo lazy !!!)
Oh! This project include a little rigid system(with so much bugs)

I follow the tutorial -- https://github.com/wlgys8/SRPLearn/wiki/MainLightShadow  (Thanks for wlgys8)

My unity version is 2022.3.21f1

When I follow this tutorial(unity version 2019.4.16), shadow map can't work.

In my unity version, I set texture format as RenderTextureFormat.Shadowmap, so I can't use UNITY_DECLARE_TEX2D and UNITY_SAMPLE_TEX2D to get data from shadow map.
It has two way to solve:
    1、 use UNITY_DECLARE_SHADOWMAP and UNITY_SAMPLE_SHADOW
    2、 change format to RenderTextureFormat.Depth
    (I choose 1)