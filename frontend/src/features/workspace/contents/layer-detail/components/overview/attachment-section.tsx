import { Paperclip, Plus, File, FileImage, FileText, Download, Trash2, Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";

interface Attachment {
  id: string;
  name: string;
  size: string;
  type: string;
  uploadDate: string;
}

const MOCK_ATTACHMENTS: Attachment[] = [
  { id: "1", name: "Strategic_Overview_v2.pdf", size: "2.4 MB", type: "pdf", uploadDate: "2024-05-01" },
  { id: "2", name: "System_Architecture.png", size: "1.1 MB", type: "image", uploadDate: "2024-05-02" },
  { id: "3", name: "Requirements_Draft.docx", size: "450 KB", type: "docx", uploadDate: "2024-05-03" },
];

export function AttachmentSection() {
  const [attachments, setAttachments] = useState<Attachment[]>(MOCK_ATTACHMENTS);
  const [isUploading, setIsUploading] = useState(false);

  const handleUpload = () => {
    setIsUploading(true);
    setTimeout(() => {
      const newFile: Attachment = {
        id: Math.random().toString(),
        name: "New_Attachment.txt",
        size: "12 KB",
        type: "txt",
        uploadDate: new Date().toISOString().split('T')[0]
      };
      setAttachments([newFile, ...attachments]);
      setIsUploading(false);
    }, 1500);
  };

  const handleDelete = (id: string) => {
    setAttachments(attachments.filter(a => a.id !== id));
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between px-1">
        <div className="flex items-center gap-2 text-muted-foreground/30">
          <Paperclip className="h-3 w-3" />
          <span className="text-[10px] font-black uppercase tracking-[0.2em]">Assets</span>
        </div>
        
        <Button 
          variant="ghost" 
          size="sm" 
          className="h-6 px-1.5 text-[9px] font-black uppercase tracking-widest text-primary/40 hover:text-primary hover:bg-primary/5 gap-1.5 rounded-md"
          onClick={handleUpload}
          disabled={isUploading}
        >
          {isUploading ? <Loader2 className="h-2.5 w-2.5 animate-spin" /> : <Plus className="h-2.5 w-2.5" />}
          {isUploading ? "..." : "Add"}
        </Button>
      </div>

      <div className="flex flex-col">
        {attachments.map((file) => (
          <AttachmentRow 
            key={file.id} 
            file={file} 
            onDelete={() => handleDelete(file.id)} 
          />
        ))}
        {attachments.length === 0 && !isUploading && (
          <div className="py-12 flex flex-col items-center justify-center border border-dashed border-border/10 rounded-md bg-muted/5 opacity-40">
            <span className="text-[10px] font-bold uppercase tracking-widest">No assets</span>
          </div>
        )}
      </div>
    </div>
  );
}

function AttachmentRow({ file, onDelete }: { file: Attachment, onDelete: () => void }) {
  const Icon = getFileIcon(file.type);

  return (
    <div className="group flex items-center justify-between py-2 px-1 hover:bg-muted/30 rounded-md transition-all cursor-pointer border border-transparent hover:border-border/10">
      <div className="flex items-center gap-3 overflow-hidden">
        <div className="h-7 w-7 rounded-md bg-muted/20 border border-border/5 flex items-center justify-center text-muted-foreground/30 group-hover:text-primary transition-colors flex-shrink-0">
          <Icon className="h-3.5 w-3.5" />
        </div>
        <div className="flex flex-col min-w-0">
          <span className="text-[11px] font-bold text-foreground/70 group-hover:text-foreground truncate transition-colors">{file.name}</span>
          <div className="flex items-center gap-2 text-[9px] font-medium text-muted-foreground/20 uppercase tracking-tighter">
            <span>{file.size}</span>
            <span className="h-0.5 w-0.5 rounded-full bg-muted-foreground/10" />
            <span>{file.uploadDate}</span>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
        <button className="h-6 w-6 flex items-center justify-center rounded-md hover:bg-background/80 text-muted-foreground/30 hover:text-foreground transition-colors">
          <Download className="h-3 w-3" />
        </button>
        <button 
          className="h-6 w-6 flex items-center justify-center rounded-md hover:bg-destructive/10 text-muted-foreground/30 hover:text-destructive transition-colors"
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
        >
          <Trash2 className="h-3 w-3" />
        </button>
      </div>
    </div>
  );
}

function getFileIcon(type: string) {
  if (type === 'image') return FileImage;
  if (type === 'pdf') return FileText;
  return File;
}
