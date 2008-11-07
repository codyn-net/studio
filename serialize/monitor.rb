require 'serialize/object'

module Cpg::Serialize
	class Hook
		include Object
		
		property :id, :name, :x, :y
	end
	
	class Monitor < Hook
		property :ymin, :ymax
	end
	
	class Control < Hook
	end
end
