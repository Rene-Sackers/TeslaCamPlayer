const gulp = require("gulp");
const sourcemaps = require("gulp-sourcemaps");

const sass = require("gulp-sass")(require("sass"));
const sassGlob = require("gulp-sass-glob");
const autoprefixer = require("gulp-autoprefixer");
const rename = require("gulp-rename");

const paths = {
	scss: {
		base: "wwwroot/scss/",
		src: "wwwroot/scss/**/*.scss"
	},
	css: {
		minifySrc: ["css/**/*.css", "!css/**/*.min.css"],
		src: "wwwroot/css/**/*.css",
		dest: "wwwroot/css/"
	}
};

function buildScss() {
	return gulp.src(paths.scss.src, { base: paths.scss.base })
		.pipe(sourcemaps.init())
		.pipe(sassGlob())
		.pipe(sass())
		.pipe(autoprefixer())
		.pipe(sourcemaps.write())
		.pipe(gulp.dest(paths.css.dest));
}

function minifyStyles() {
	return gulp.src(paths.css.minifySrc)
		.pipe(cleanCss())
		.pipe(rename({ extname: ".min.css" }))
		.pipe(gulp.dest(paths.publish.dest));
}

function watch() {
	gulp.watch(paths.scss.src, buildScss);
}

exports.styles = buildScss;
exports.watch = watch;

exports.default = buildScss;
exports.publish = gulp.series(buildScss, minifyStyles);