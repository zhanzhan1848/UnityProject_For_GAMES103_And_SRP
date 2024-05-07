# UnityProject_For_GAMES103_And_SRP

This is cascade shadow map project, and shallow wave for fluid
Oh! This project include a little rigid system(with so much bugs)

I follow the tutorial -- https://github.com/wlgys8/SRPLearn/wiki/CascadeShadowMapping  (Thanks for wlgys8)

My unity version is 2022.3.21f1

When I follow this tutorial(unity version 2019.4.16), shadow map can't work.

In my unity version, I set texture format as RenderTextureFormat.Shadowmap, so I can't use UNITY_DECLARE_TEX2D and UNITY_SAMPLE_TEX2D to get data from shadow map.
It has two way to solve:
    1、 use UNITY_DECLARE_SHADOWMAP and UNITY_SAMPLE_SHADOW
    2、 change format to RenderTextureFormat.Depth
    (I choose 1)

And the toe-way coupling between fluid and rigid body has not implement

The shadow result:(Shader has some bug, so shadow is wrong)
![image](https://github.com/zhanzhan1848/UnityProject_For_GAMES103_And_SRP/blob/Shallow-wave-and-cascade-shadow-map/cascade.png)


The shallow wave result:(May be hard to see wave because of shader's bug)
![image](https://github.com/zhanzhan1848/UnityProject_For_GAMES103_And_SRP/blob/Shallow-wave-and-cascade-shadow-map/result.git)