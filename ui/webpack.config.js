const path=require('path');
const localDeps=path.resolve(__dirname,'node_modules');
const deps=require('fs').existsSync(path.join(localDeps,'webpack'))?localDeps:path.resolve(__dirname,'../../GizmoXYZ/ui/node_modules');
const webpack=require(path.join(deps,'webpack'));
const MiniCssExtractPlugin=require(path.join(deps,'mini-css-extract-plugin'));
const TerserPlugin=require(path.join(deps,'terser-webpack-plugin'));
module.exports={optimization:{minimizer:[new TerserPlugin({extractComments:false})]},mode:'production',entry:'./index.js',resolve:{modules:[deps,'node_modules']},resolveLoader:{modules:[deps,'node_modules']},externalsType:'window',externals:{react:'React','cs2/api':'cs2/api','cs2/l10n':'cs2/l10n','cs2/ui':'cs2/ui'},experiments:{outputModule:true},output:{path:path.resolve(__dirname,'../local/StairBuilder'),filename:'StairBuilder.mjs',library:{type:'module'},publicPath:'coui://ui-mods/'},module:{rules:[{test:/\.css$/,use:[MiniCssExtractPlugin.loader,{loader:'css-loader',options:{modules:true}}]},{test:/\.svg$/,type:'asset/resource',generator:{filename:'images/StairBuilder/[name][ext]'}}]},plugins:[new MiniCssExtractPlugin({filename:'StairBuilder.css'}),new webpack.BannerPlugin({test:/\.mjs$/,banner:'Cities: Skylines II UI Module\nId: StairBuilder\nAuthor: zacum\nVersion: 0.9.14\nDependencies: '})]};


