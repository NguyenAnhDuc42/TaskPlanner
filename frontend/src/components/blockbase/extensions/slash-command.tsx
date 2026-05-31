import { Extension } from '@tiptap/core';
import type { Editor, Range } from '@tiptap/core';
import Suggestion from '@tiptap/suggestion';
import type { SuggestionProps, SuggestionKeyDownProps } from '@tiptap/suggestion';
import { ReactRenderer } from '@tiptap/react';
import tippy, { type Instance as TippyInstance } from 'tippy.js';
import React, { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import {
  Heading1, Heading2, Heading3,
  List, ListOrdered, CheckSquare,
  Code, Quote, Minus, Type,
} from 'lucide-react';
import { cn } from '@/lib/utils';

export interface CommandItemProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  command: (props: { editor: Editor; range: Range }) => void;
}

interface CommandListRef {
  onKeyDown: (props: SuggestionKeyDownProps) => boolean;
}

const COMMAND_GROUPS: { label: string; items: CommandItemProps[] }[] = [
  {
    label: 'Text',
    items: [
      {
        title: 'Text',
        description: 'Plain paragraph',
        icon: <Type className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).setNode('paragraph').run(),
      },
      {
        title: 'Heading 1',
        description: 'Large section heading',
        icon: <Heading1 className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 1 }).run(),
      },
      {
        title: 'Heading 2',
        description: 'Medium section heading',
        icon: <Heading2 className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 2 }).run(),
      },
      {
        title: 'Heading 3',
        description: 'Small section heading',
        icon: <Heading3 className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 3 }).run(),
      },
    ],
  },
  {
    label: 'Lists',
    items: [
      {
        title: 'Bullet List',
        description: 'Simple bulleted list',
        icon: <List className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).toggleBulletList().run(),
      },
      {
        title: 'Numbered List',
        description: 'Ordered numbered list',
        icon: <ListOrdered className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).toggleOrderedList().run(),
      },
      {
        title: 'To-do List',
        description: 'Track tasks with checkboxes',
        icon: <CheckSquare className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).toggleTaskList().run(),
      },
    ],
  },
  {
    label: 'Blocks',
    items: [
      {
        title: 'Quote',
        description: 'Capture a quote or callout',
        icon: <Quote className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).toggleBlockquote().run(),
      },
      {
        title: 'Code Block',
        description: 'Syntax-highlighted code',
        icon: <Code className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).toggleCodeBlock().run(),
      },
      {
        title: 'Divider',
        description: 'Visual section separator',
        icon: <Minus className="h-3.5 w-3.5" />,
        command: ({ editor, range }) =>
          editor.chain().focus().deleteRange(range).setHorizontalRule().run(),
      },
    ],
  },
];

export const getSuggestionItems = ({ query }: { query: string }): CommandItemProps[] => {
  const q = query.toLowerCase();
  return COMMAND_GROUPS.flatMap((g) => g.items).filter(
    (item) =>
      item.title.toLowerCase().includes(q) || item.description.toLowerCase().includes(q)
  );
};

export const CommandList = forwardRef(
  (
    props: { items: CommandItemProps[]; command: (item: CommandItemProps) => void },
    ref
  ) => {
    const [selectedIndex, setSelectedIndex] = useState(0);

    const selectItem = (index: number) => {
      const item = props.items[index];
      if (item) props.command(item);
    };

    useEffect(() => {
      setSelectedIndex(0);
    }, [props.items]);

    useImperativeHandle(ref, () => ({
      onKeyDown: ({ event }: { event: KeyboardEvent }) => {
        if (event.key === 'ArrowUp') {
          setSelectedIndex((i) => (i + props.items.length - 1) % props.items.length);
          return true;
        }
        if (event.key === 'ArrowDown') {
          setSelectedIndex((i) => (i + 1) % props.items.length);
          return true;
        }
        if (event.key === 'Enter') {
          selectItem(selectedIndex);
          return true;
        }
        return false;
      },
    }));

    if (props.items.length === 0) return null;

    // Group displayed items
    const displayedGroups = COMMAND_GROUPS.map((g) => ({
      ...g,
      items: g.items.filter((item) => props.items.includes(item)),
    })).filter((g) => g.items.length > 0);

    let globalIndex = 0;

    return (
      <div className="z-50 min-w-[260px] max-h-[380px] overflow-y-auto bg-background/98 backdrop-blur-xl rounded-xl shadow-2xl border border-border/40 p-1.5 flex flex-col gap-0.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-border/30 [&::-webkit-scrollbar-thumb]:rounded-full">
        {displayedGroups.map((group) => (
          <div key={group.label}>
            <div className="text-[9px] font-black uppercase tracking-widest text-muted-foreground/40 px-2 pt-2 pb-1 font-mono">
              {group.label}
            </div>
            {group.items.map((item) => {
              const idx = globalIndex++;
              return (
                <button
                  key={item.title}
                  className={cn(
                    'flex items-center gap-2.5 px-2 py-1.5 rounded-lg text-left transition-all w-full group',
                    idx === selectedIndex
                      ? 'bg-primary/10 text-foreground'
                      : 'hover:bg-muted/50 text-foreground/80'
                  )}
                  onClick={() => selectItem(idx)}
                  type="button"
                >
                  <div
                    className={cn(
                      'flex items-center justify-center h-7 w-7 rounded-md border shrink-0 transition-all',
                      idx === selectedIndex
                        ? 'bg-primary/10 border-primary/30 text-primary'
                        : 'bg-muted/40 border-border/30 text-foreground/60 group-hover:border-border/60'
                    )}
                  >
                    {item.icon}
                  </div>
                  <div className="flex flex-col min-w-0">
                    <span className="text-[12px] font-semibold leading-tight">{item.title}</span>
                    <span className="text-[10px] text-muted-foreground/60 truncate">{item.description}</span>
                  </div>
                </button>
              );
            })}
          </div>
        ))}
      </div>
    );
  }
);

CommandList.displayName = 'CommandList';

export const renderSuggestion = () => {
  let component: ReactRenderer;
  let popup: TippyInstance;

  return {
    onStart: (props: SuggestionProps) => {
      component = new ReactRenderer(CommandList, { props, editor: props.editor });
      if (!props.clientRect) return;
      popup = tippy(document.body, {
        getReferenceClientRect: props.clientRect as () => DOMRect,
        appendTo: () => document.body,
        content: component.element,
        showOnCreate: true,
        interactive: true,
        trigger: 'manual',
        placement: 'bottom-start',
        animation: 'shift-away',
        duration: [120, 80],
      });
    },
    onUpdate(props: SuggestionProps) {
      component.updateProps(props);
      if (!props.clientRect) return;
      popup.setProps({ getReferenceClientRect: props.clientRect as () => DOMRect });
    },
    onKeyDown(props: SuggestionKeyDownProps) {
      if (props.event.key === 'Escape') { popup.hide(); return true; }
      return (component.ref as CommandListRef)?.onKeyDown(props);
    },
    onExit() {
      popup.destroy();
      component.destroy();
    },
  };
};

export const SlashCommand = Extension.create({
  name: 'slashCommand',
  addOptions() {
    return {
      suggestion: {
        char: '/',
        command: ({ editor, range, props }: { editor: Editor; range: Range; props: CommandItemProps }) => {
          props.command({ editor, range });
        },
      },
    };
  },
  addProseMirrorPlugins() {
    return [Suggestion({ editor: this.editor, ...this.options.suggestion })];
  },
});
