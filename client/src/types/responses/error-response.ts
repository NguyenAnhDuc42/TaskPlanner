export interface ErrorResponse{
    type:string
    title:string
    status:number
    detail?:string
    instance?:string
    extensions?:Record<string,string[]>  
}
