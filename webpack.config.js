var path = require('path');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require('mini-css-extract-plugin');


function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}

var cfg = {
    projectPath: './src/Demo/',
    index: './src/Demo/index.html',
    entry: './fable_output/Main.js',
    output: './dist'
}

// CopyWebpackPlugin format
cfg.copy = [
    'public',
    'node_modules/bulma/css/bulma.min.css',
    {
        from: 'node_modules/@fortawesome/fontawesome-free/css/all.min.css',
        to: 'fa/css/[name][ext]'
    },
    {
        from: 'node_modules/@fortawesome/fontawesome-free/webfonts/*',
        to: 'fa/webfonts/[name][ext]'
    }
]

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var htmlPlugin =
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve(cfg.index)
    });

// Copies static assets to output directory
var copyPlugin =
    new CopyWebpackPlugin({
        patterns: cfg.copy
    });

// CSS bundling
var cssPlugin =
    new MiniCssExtractPlugin({
        filename: '[name].[contenthash].css'
    });


// Configuration for webpack-dev-server
var devServer = {
    host: 'localhost',
    port: 8888,
    hot: true,
    // hot: false,
    // liveReload: false,
    client: {overlay: false}
};

// If we're running the webpack-dev-server, assume we're in development mode
var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
var environment = isProduction ? 'production' : 'development';
process.env.NODE_ENV = environment;
console.log('Bundling for ' + environment + '...');

module.exports = {
    entry: { app: resolve(cfg.entry) },
    output: {
        path: resolve(cfg.output),
        filename: isProduction ? '[name].[contenthash].js' : '[name].js',
        clean: true
    },
    devtool: isProduction ? undefined : 'eval-source-map',
    resolve: {
        alias: {
            Project: path.resolve(__dirname, cfg.projectPath)
        },
        symlinks: false
    }, // See https://github.com/fable-compiler/Fable/issues/1490
    mode: environment,
    plugins: isProduction ? [cssPlugin, htmlPlugin, copyPlugin] : [htmlPlugin, copyPlugin],
    optimization: { splitChunks: { chunks: 'all' } },
    devServer: devServer,
    module: {
        rules: [
            {
                test: /\.js$/,
                enforce: "pre",
                exclude: /node_modules|fable_modules/,
                use: ["source-map-loader"]
            },
            {
                test: /\.(sass|scss|css)$/,
                use: [
                    isProduction
                        ? MiniCssExtractPlugin.loader
                        : 'style-loader',
                    {
                        loader: 'css-loader',
                        // otherwise it breaks css urls
                        options: {url: false}
                    },

                ]
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ['file-loader']
            }
        ]
    }
};