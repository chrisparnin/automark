automark
========

Automark enables reviewing, sharing, and summarizing programming tasks by automatically 
generating markdown from your coding history.

The auto-generated markdown can help you hand-off tasks to other teammates, 
recover from an interruption, or document how to perform a similar task in the future.

Blog posts can be created from the markdown as well.


### Motivation

Developers commonly blog about how-to tasks and development experiences in programming.
But not everyone blogs, and the ones that do often spend several hours recalling, formatting, and crafting a blog post.

The goal of automark is to reduce the friction associated with blogging, make it more habitual, and 
allow developers to focus on the learning experiences and narrative of a blog post.

### Features

- Generates a series of time-ordered code snippets based on order of creation.
- Code snippets intelligently chunked based on location, size, and change density.
- Includes referenced resources, such as visited Stack Overflow questions or official documentation pages used.
- Option to generate failed attempts to get a code snippet to work (and ultimate final solution).


#### Review Mode

Markdown is generated to show unified diffs.
![ReviewMode](https://raw.github.com/chrisparnin/automark/master/Doc/ReviewMode.png)

#### Share Mode

**In the works:** Format more suitable for sharing recent coding history.

### Future Features

- Natural language summaries of code snippets.
- [Create an issue for new feature request](https://github.com/chrisparnin/automark/issues/new).
