# GPUDrivenPipeline

#### 介绍
一套GPU驱动的渲染管线

#### 说明

- 基于unitySRP
- 视锥剔除
- hiz遮挡剔除
- 完成cluster划分：参照nanite源码，将算法封装成c++动态库，在unity中实现调用

![image-20240103180820811](C:\Users\QZDZ\AppData\Roaming\Typora\typora-user-images\image-20240103180820811.png)
