const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const Dotenv = require('dotenv-webpack');

const rootConfig = {
  mode: 'development',
  optimization: {
    usedExports: true, // tells webpack to tree-shake
  },
  devtool: 'eval-source-map',
};

const rulesConfig = {
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
      {
        test: /\.css$/i,
        use: [MiniCssExtractPlugin.loader,'css-loader'],
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js', '.css'],
  },
}

const vendorConfig = {
  optimization: {
    usedExports: true, // tells webpack to tree-shake
    runtimeChunk: 'single',
    splitChunks: {
      cacheGroups: {
        vendor: {
          test: /[\\/]node_modules[\\/]/,
          name: 'vendors',
          chunks: 'all',
        }
      }
    }
  }
}

module.exports = {
  ...rootConfig,
  ...vendorConfig,
  entry: './src/index.ts',
  ...rulesConfig,
  plugins: [
    new MiniCssExtractPlugin(),
    new HtmlWebpackPlugin({
      filename: "index.html",
      title: "ACP Example",
      template: "src/index.html",
    }),
    new HtmlWebpackPlugin({
      filename: "404.html",
      title: "Page not found",
      template: "src/404.html",
      inject: false
    }),
    new Dotenv({
      path: `./.env`,
    }),
  ],
  output: {
    filename: '[name].bundle.js',
    path: path.resolve(__dirname, 'public'),
    library: 'User'
  },
}
