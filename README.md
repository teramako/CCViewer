# CCViewer
Concole Comic Viewer

![sample](docs/img/001.png)

## Syntax

```
ccv [ { -j | --jcomic } ] [ { -p | --page } <number> ] path/to/.zip
ccv [ { -j | --jcomic } ] [ { -p | --page } <number> ] path/to/directory
ccv [ { -j | --jcomic } ] [ { -p | --page } <number> ] path/to/image file [path/to/image file ...]
```

| Option            | Type    | Default | Description                                          |
|:------------------|:--------|:--------|:-----------------------------------------------------|
| `-p`, `--page`    | int     | 1       | Page number to display (starting with 1)")]          |
| `-j`, `--jcommic` | switch  | false   | Japanese Commic Mode (Page direction: Right to Left) |


## Key bindings

| Key                  | Description                                   |
|:---------------------|:----------------------------------------------|
| `q`                  | Quit                                          |
| `n`, `Ctrl+n`        | Move Next page                                |
| `N`                  | Move Next page (force single page)            |
| `p`, `Ctrl+p`        | Move Previous page                            |
| `P`                  | Move Previous page (force single page)        |
| `Esc`, `r`, `Ctrl+r` | Re-rendering the current page(s)              |
| `s`                  | Re-rendering (force single page)              |
| `^`                  | Move the first page                           |
| `$`                  | Move the last page                            |
| `m`                  | Toggle Page mode                              |
| `:`                  | Enter the comand mode                         |

### Command mode

- _num_: Jump to page number of _num_ (absolute)
- `+` _num_: Jump to the advanced page by _num_ (relative)
- `-` _num_: Jump to the previous page by _num_ (relative)

