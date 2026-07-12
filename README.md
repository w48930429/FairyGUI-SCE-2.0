# FairyGUI-SCE-2.0
fairygui in sce 2.0

部分fairgui的控件类型无法实现，比如:。
因为是使用sce的UI控件来实现相关效果。
fairygui的图形里很多效果也无法实现，只能基础的绘制普通的方形图片。
UBB语法无法实现。

GImage采用Canvas实现而不是Control，是因为Control本身针对图片的alpha显示效果不行。
基于上面那点，GMovieClip 的效果只能由Canvas来实现。
GGraph（椭圆） 是因为Control只能实现 方形图片，不能自渲染图形。

有适配（使用了fgui的关联）。

建议让AI去读取文档并实现。
---

(https://github.com/claudeskydream-hash/FairyGUI-spark.git)

