automark
========

`automark` enables reviewing, sharing, and summarizing programming tasks by automatically 
generating markdown from your coding history.

The auto-generated markdown or rendered html can help you:

1. Hand-off tasks to other teammates, 
2. Recover from an interruption, or 
3. Document how to perform a similar task in the future.

It is available as a [Visual Studio extension](http://visualstudiogallery.msdn.microsoft.com/078d00b7-dfbd-4cfa-97f9-8be08bb510ee) in the Visual Studio gallery.

### Motivation

Developers commonly blog about how-to tasks and development experiences in programming.
But not everyone blogs, and the ones that do often spend several hours recalling, formatting, and crafting a blog post. The goal of automark is to 

1. reduce the friction associated with blogging, 
2. make it more habitual, and 
3. allow developers to focus on the narrative of a blog post.

Example output created with `automark`:

![DiffHighlight](https://raw.github.com/chrisparnin/automark/master/Doc/DiffHighlight.png)

See the full example of a [blog post created with automark](http://chrisparnin.github.io/articles/2013/10/creating-a-visual-studio-extension-shim-for-automark/).

### Features

Automark is designed to support *episodic review*, a cognitive process for 
walking through and reasoning about recent code changes, mistakes, and events.

To support this, automark can

- read a `autogit` repository and generates a series of time-ordered code changes.
- support for rendering and displaying unified diffs.
- include additional references, such as visited Stack Overflow questions or official documentation pages used.
- generate markdown or html


### Using automark

To use automark, first ensure that [autogit](https://github.com/chrisparnin/autogit) has been installed first.

![Menu](https://raw.github.com/chrisparnin/automark/master/Doc/menu.png)

To generate a markdown representation of the recent coding task, select Tools -> automark -> Generate Markdown.  This will generate a markdown file stored in `$SolutionFolder\.HistoryData\md\Timestamp.md` and then open it in an markdown editor.  For windows, I recommend using [Markdown Pro](http://www.markdownpro.com/).  From here, the markdown can be annotated and further later generated as a html.

![Markdown](https://raw.github.com/chrisparnin/automark/master/Doc/markdown.png)

To generate a rendered html representation, including diff highlighting, select Tools -> automark -> Generate Html.

![Html](https://raw.github.com/chrisparnin/automark/master/Doc/html.png)

### Installing automark

- [Download .vsix](http://visualstudiogallery.msdn.microsoft.com/078d00b7-dfbd-4cfa-97f9-8be08bb510ee) and double click to install, or search for "automark" in Online Gallery in "Visual Studio's Tools > Extensions and Updates" tool menu.

- To install from source, first install the [Visual Studio SDK 2012](http://www.microsoft.com/en-us/download/details.aspx?id=30668) in order to build project. Then install the resulting .vsix file.

### Future Features

- Natural language summaries of code snippets.
- Blog connectors, deploy to github pages
- [Create an issue for new feature request](https://github.com/chrisparnin/automark/issues/new).
