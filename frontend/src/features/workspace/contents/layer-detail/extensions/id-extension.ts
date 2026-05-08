import { Extension } from "@tiptap/react";

// Custom extension to keep track of DB IDs on Tiptap nodes
export const IdExtension = Extension.create({
  name: "idTracking",
  addGlobalAttributes() {
    return [
      {
        types: ["paragraph", "heading", "taskItem"],
        attributes: {
          id: {
            default: null,
            parseHTML: element => element.getAttribute("data-id"),
            renderHTML: attributes => {
              if (!attributes.id) return {};
              return { "data-id": attributes.id };
            },
            keepOnSplit: false,
          },
        },
      },
    ];
  },
});
