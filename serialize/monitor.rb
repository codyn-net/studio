require 'serialize/object'

module Cpg::Serialize
	class Monitor
		include Object
		
		property :id, :name, :ymin, :ymax
	end
end
